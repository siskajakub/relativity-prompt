using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Relativity.API;
using Relativity.Kepler.Transport;
using Relativity.Services.Objects;
using Relativity.Services.Objects.DataContracts;

namespace RelativityPrompt
{
    [Description("Prompt")]
    [Guid("80216E86-E5E3-409B-8896-CAC97637F042")]

    /*
     * Relativity Mass EventHandler Class
     */
    public class MassOperationHandler : kCura.MassOperationHandlers.MassOperationHandler
    {
        /*
         * Occurs after the user has selected items and pressed go.
         * In this function you can validate the items selected and return a warning/error message.
         */
        public override kCura.EventHandler.Response ValidateSelection()
        {
            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<MassOperationHandler>();

            // Init general response
            kCura.EventHandler.Response response = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = ""
            };

            // Get current Workspace ID
            int workspaceId = this.Helper.GetActiveCaseID();
            _logger.LogDebug("Prompt, current Workspace ID: {workspaceId}", workspaceId.ToString());

            // Check if all Instance Settings are in place
            IDictionary<string, string> instanceSettings = this.GetInstanceSettings(ref response, new string[] { "SourceField", "DestinationField", "LogField", "OpenAIKey", "OpenAIEndpoint", "Model", "Prompt", "PromptMaxSize" });
            // Check if there was not error
            if (!response.Success)
            {
                return response;
            }

            // Preview the prompt
            response.Message = string.Format("PROMPT\n\n{0}\n\n++DOCUMENT[{1}]++", instanceSettings["Prompt"], instanceSettings["SourceField"]);

            return response;
        }

        /*
         * Occurs after the user has inputted data to a layout and pressed OK.
         * This function runs as a pre-save eventhandler.
         * This is NOT called if the mass operation does not have a layout.
         */
        public override kCura.EventHandler.Response ValidateLayout()
        {
            // Init general response
            kCura.EventHandler.Response response = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = ""
            };
            return response;
        }

        /*
         * Occurs before batching begins. A sample use would be to set up an instance of an object.
         */
        public override kCura.EventHandler.Response PreMassOperation()
        {
            // Init general response
            kCura.EventHandler.Response response = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = ""
            };
            return response;
        }

        /*
         * This function is called in batches based on the size defined in configuration.
         */
        public override kCura.EventHandler.Response DoBatch()
        {
            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<MassOperationHandler>();

            // Init general response
            kCura.EventHandler.Response response = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = ""
            };

            // Get current Workspace ID
            int workspaceId = this.Helper.GetActiveCaseID();
            _logger.LogDebug("Prompt, current Workspace ID: {workspaceId}", workspaceId.ToString());

            // Check if all Instance Settings are in place
            IDictionary<string, string> instanceSettings = this.GetInstanceSettings(ref response, new string[] { "SourceField", "DestinationField", "LogField", "OpenAIKey", "OpenAIEndpoint", "Model", "Prompt", "PromptMaxSize" });
            // Check if there was not error
            if (!response.Success)
            {
                return response;
            }

            // Update general status
            this.ChangeStatus("Prompting documents");

            // For each document create prompting task
            List<Task<int>> promptTasks = new List<Task<int>>();
            int runningTasks = 0;
            int concurrentTasks = 16;
            for (int i = 0; i < this.BatchIDs.Count; i++)
            {
                // Prompt documents and update Relativity using Object Manager API
                promptTasks.Add(PromptDocument(workspaceId, this.BatchIDs[i], instanceSettings["SourceField"], instanceSettings["DestinationField"], instanceSettings["LogField"], instanceSettings["OpenAIKey"], instanceSettings["OpenAIEndpoint"], instanceSettings["Model"], instanceSettings["Prompt"], int.Parse(instanceSettings["PromptMaxSize"])));

                // Update progreass bar
                this.IncrementCount(1);

                // Allow only certain number of tasks to run concurrently
                do
                {
                    runningTasks = 0;
                    foreach (Task<int> promptTask in promptTasks)
                    {
                        if (!promptTask.IsCompleted)
                        {
                            runningTasks++;
                        }
                    }
                    if (runningTasks >= concurrentTasks)
                    {
                        Thread.Sleep(100);
                    }
                } while (runningTasks >= concurrentTasks);
            }

            // Update general status
            this.ChangeStatus("Waiting to finish the document prompting");

            // Wait for all prompting to finish
            _logger.LogDebug("Prompt, waiting for all documents finish prompting ({n} document(s))", this.BatchIDs.Count.ToString());
            Task.WaitAll(promptTasks.ToArray());

            // Update general status
            this.ChangeStatus("Checking the results of the document prompting");

            // Check results
            List<string> promptingErrors = new List<string>();
            for (int i = 0; i < promptTasks.Count; i++)
            {
                // If prompting was not done add to the error List
                _logger.LogDebug("Prompt, prompting task result: {result} (task: {task})", promptTasks[i].Result.ToString(), promptTasks[i].Id.ToString());
                if (promptTasks[i].Result != 0)
                {
                    promptingErrors.Add(promptTasks[i].Result.ToString());
                }
            }

            // If there are any errors adjust response
            if (promptingErrors.Count > 0)
            {
                _logger.LogError("Prompt, not all documents have been prompted: ({documents})", string.Join(", ", promptingErrors));

                response.Success = false;
                response.Message = "Not all documents have been prompted";
            }

            return response;
        }

        /*
         * Occurs after all batching is completed.
         */
        public override kCura.EventHandler.Response PostMassOperation()
        {
            // Init general response
            kCura.EventHandler.Response response = new kCura.EventHandler.Response()
            {
                Success = true,
                Message = ""
            };
            return response;
        }

        /*
         * Custom method to get required Relativity Instance Settings
         */
        private IDictionary<string, string> GetInstanceSettings(ref kCura.EventHandler.Response response, string[] instanceSettingsNames)
        {
            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<MassOperationHandler>();

            // Output Dictionary
            IDictionary<string, string> instanceSettingsValues = new Dictionary<string, string>();

            // Get and validate instance settings
            foreach (string name in instanceSettingsNames)
            {
                try
                {
                    instanceSettingsValues.Add(name, this.Helper.GetInstanceSettingBundle().GetString("Prompt", name));
                    if (instanceSettingsValues[name].Length <= 0)
                    {
                        _logger.LogError("Prompt, Instance Settings empty error: {section}/{name}", "Prompt", name);

                        response.Success = false;
                        response.Message = "Instance Settings error";
                        return instanceSettingsValues;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Prompt, Instance Settings error: {section}/{name}", "Prompt", name);

                    response.Success = false;
                    response.Message = "Instance Settings error";
                    return instanceSettingsValues;
                }

                _logger.LogDebug("Prompt, Instance Setting: {name}=>{value}", name, instanceSettingsValues[name]);
            }

            // Check PromptMaxSize Instance Settings is a integer
            try
            {
                int.Parse(instanceSettingsValues["PromptMaxSize"]);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Promptr, Instance Settings error: {section}/{name}", "Prompt", "PromptMaxSize");

                response.Success = false;
                response.Message = "Instance Settings error";
                return instanceSettingsValues;
            }

            return instanceSettingsValues;
        }

        /*
         * Custom method to translate document using Azure Translator
         */
        private async Task<int> PromptDocument(int workspaceId, int documentArtifactId, string sourceField, string destinationField, string logField, string openAIKey, string openAIEndpoint, string model, string prompt, int promptMaxSize)
        {
            /*
             * Custom local function to shorten string based on threshold
             */
            string Shortener(string str, char[] delimiters, int threshold)
            {
                string chunk = "";

                int len = str.Length;

                if (len <= threshold)
                {
                    return str;
                }

                for (int i = threshold; i > 0; i--)
                {
                    if (delimiters.Contains(str[i]))
                    {
                        chunk = str.Substring(0, i);
                        break;
                    }
                }

                return chunk;
            }
            
            // Get logger
            Relativity.API.IAPILog _logger = this.Helper.GetLoggerFactory().GetLogger().ForContext<MassOperationHandler>();

            // Get Relativity Object Manager API
            IObjectManager objectManager = this.Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.CurrentUser);

            // Get document text
            Stream stream;
            try
            {
                // Construct objects and retreive document content
                RelativityObjectRef relativityObject = new RelativityObjectRef
                {
                    ArtifactID = documentArtifactId
                };
                FieldRef relativityField = new FieldRef
                {
                    Name = sourceField
                };
                IKeplerStream keplerStream = await objectManager.StreamLongTextAsync(workspaceId, relativityObject, relativityField);
                stream = await keplerStream.GetStreamAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Prompt, document for prompting retrieval error (ArtifactID: {id})", documentArtifactId.ToString());
                return documentArtifactId;
            }

            // Create full prompt by putting together prompt template and document text
            string promptDocument = prompt + "\n\n\n" + new StreamReader(stream).ReadToEnd();

            // Log original document
            _logger.LogDebug("Prompt, original document (ArtifactID: {id}, length: {length})", documentArtifactId.ToString(), promptDocument.Length.ToString());

            // Limit document text size to align with model's context size or limit costs associated with prompting
            promptDocument = Shortener(promptDocument, new char[] { '.', ' '}, promptMaxSize);
            _logger.LogDebug("Prompt, document text shortened to {n} characters (ArtifactID: {id})", promptDocument.Length.ToString(), documentArtifactId.ToString());

            // Force TLS 1.2 or higher as Azure requires it
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 & ~(SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11);

            // crete request body
            var requestBody = new
            {
                model = model,
                prompt = promptDocument
            };

            // Do prompt call
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(openAIEndpoint + "/v1/completions");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", $"Bearer {openAIKey}");

            // Send the request
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

            // Check the response
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Prompt, HTTP reposnse error (ArtifactID: {id}, status: {status})", documentArtifactId.ToString(), response.StatusCode.ToString());
                return documentArtifactId;
            }

            // Read the response
            string promptResponse = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Prompt, prompting result JSON check (ArtifactID: {id}, length: {length})", documentArtifactId.ToString(), promptResponse.Length.ToString());
            client.Dispose();

            // Parse JSON
            OpenAIResponse promptResults = JsonSerializer.Deserialize<OpenAIResponse>(promptResponse);

            // Check the result
            if (promptResults.Choices.Length == 0 || promptResults.Choices[0].Text.Length == 0)
            {
                _logger.LogError("Prompt, empty prompt result (ArtifactID: {id})", documentArtifactId.ToString());
                return documentArtifactId;
            }

            // Log the prompt result
            _logger.LogDebug("Prompt, prompting result (ArtifactID: {id}, length: {length})", documentArtifactId.ToString(), promptResults.Choices[0].Text.Length.ToString());

            // Construct prompting result stream
            Stream streamPromptResults = new MemoryStream();
            StreamWriter streamWriter = new StreamWriter(streamPromptResults);
            streamWriter.Write(promptResults.Choices[0].Text);
            streamWriter.Flush();
            streamPromptResults.Position = 0;

            // Update document with prompting result
            try
            {
                // Construct objects and do document update
                RelativityObjectRef relativityObject = new RelativityObjectRef
                {
                    ArtifactID = documentArtifactId
                };
                FieldRef relativityField = new FieldRef
                {
                    Name = destinationField
                };
                UpdateLongTextFromStreamRequest updateRequest = new UpdateLongTextFromStreamRequest
                {
                    Object = relativityObject,
                    Field = relativityField
                };
                KeplerStream keplerStream = new KeplerStream(streamPromptResults);
                await objectManager.UpdateLongTextFromStreamAsync(workspaceId, updateRequest, keplerStream);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Prompt, prompting result update error (ArtifactID: {id})", documentArtifactId.ToString());
                return documentArtifactId;
            }
            
            // Update document prompting log
            try
            {
                Stream streamCurrentLog;
                // Construct objects and get current translation log
                RelativityObjectRef relativityObject = new RelativityObjectRef
                {
                    ArtifactID = documentArtifactId
                };
                FieldRef relativityField = new FieldRef
                {
                    Name = logField
                };
                IKeplerStream keplerStream = await objectManager.StreamLongTextAsync(workspaceId, relativityObject, relativityField);
                streamCurrentLog = await keplerStream.GetStreamAsync();

                // Add new translation log
                Stream streamUpdatedLog = new MemoryStream();
                StreamWriter streamLogWriter = new StreamWriter(streamUpdatedLog);
                streamLogWriter.Write(new StreamReader(streamCurrentLog).ReadToEnd());
                streamLogWriter.Write("Prompt;" + this.Helper.GetAuthenticationManager().UserInfo.EmailAddress + ";" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + ";" + model + ";" + promptDocument.Length.ToString() + ";" + promptResults.Choices[0].Text.Length.ToString() + "\n");
                streamLogWriter.Flush();
                streamUpdatedLog.Position = 0;

                // Write updated translation log
                UpdateLongTextFromStreamRequest updateRequest = new UpdateLongTextFromStreamRequest
                {
                    Object = relativityObject,
                    Field = relativityField
                };
                keplerStream = new KeplerStream(streamUpdatedLog);
                await objectManager.UpdateLongTextFromStreamAsync(workspaceId, updateRequest, keplerStream);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Prompt, prompting log update error (ArtifactID: {id})", documentArtifactId.ToString());
                return documentArtifactId;
            }
            
            // Return 0 as all went without error
            return 0;
        }
    }
}