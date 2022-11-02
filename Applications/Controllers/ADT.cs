﻿
using Azure;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UACloudTwin.Interfaces;
using UACloudTwin.Models;

namespace UACloudTwin.Controllers
{
    public class ADT : Controller
    {
        private readonly ISubscriber _subscriber;

        public static DigitalTwinsClient ADTClient { get; private set; } = null;

        public ADT(ISubscriber subscriber)
        {
            _subscriber = subscriber;
        }

        public ActionResult Index()
        {
            ADTModel adtModel = new ADTModel
            {
                StatusMessage = ""
            };

            return View("Index", adtModel);
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        [HttpPost]
        public ActionResult Login(string instanceUrl, string endpoint)
        {
            try
            {
                ADTClient = new DigitalTwinsClient(new Uri(instanceUrl), new DefaultAzureCredential());

                // read our ISA95 models
                foreach (string dtdlFilePath in Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), "ISA95"), "*.json"))
                {
                    string fileContent = System.IO.File.ReadAllText(dtdlFilePath);

                    JObject elements = JsonConvert.DeserializeObject<JObject>(fileContent);
                    string modelId = elements.First.Next.First.ToString();

                    // upload the model if it doesn't already exist
                    try
                    {
                        ADTClient.GetModel(modelId);
                    }
                    catch (RequestFailedException)
                    {
                        Response<DigitalTwinsModelData[]> response = ADTClient.CreateModels(new List<string>() { fileContent });
                    }
                }

                if (!string.IsNullOrEmpty(endpoint))
                {
                    string[] parts = endpoint.Split(';');

                    Environment.SetEnvironmentVariable("BROKER_USERNAME", "$ConnectionString");
                    Environment.SetEnvironmentVariable("BROKER_PASSWORD", endpoint);
                    Environment.SetEnvironmentVariable("BROKER_PORT", "9093");
                    Environment.SetEnvironmentVariable("CLIENT_NAME", "microsoft");
                    Environment.SetEnvironmentVariable("BROKER_NAME", parts[0].Substring(parts[0].IndexOf('=') + 6).TrimEnd('/'));
                    Environment.SetEnvironmentVariable("TOPIC", parts[3].Substring(parts[3].IndexOf('=') + 1));
                }

                _subscriber.Connect();

                ADTModel adtModel = new ADTModel
                {
                    StatusMessage = "Connection to broker and ADT service successful!"
                };

                return View("Index", adtModel);
            }
            catch (Exception ex)
            {
                ADTModel adtModel = new ADTModel
                {
                    StatusMessage = ex.Message
                };

                return View("Index", adtModel);
            }
        }
    }
}
