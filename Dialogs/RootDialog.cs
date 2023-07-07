using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Azure.Core;
using Azure;
using Azure.AI.Language.Conversations;
using System.Text.Json;

namespace Bot.Api.Dialogs
{
    public class RootDialog : ComponentDialog
    {
        private readonly ConversationState _conversationState;

        /// <summary>
        /// Initializes a new instance of the <see cref="RootDialog"/> class.
        /// </summary>
        /// <param name="conversationState">The conversation state.</param>
        public RootDialog(ConversationState conversationState)
        {
            _conversationState = conversationState;
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                GetCluIntent,
                Redirect
            }));
        }


        /// <summary>
        /// Use the CLU service
        /// </summary>
        /// <param name="stepContext">WaterfallStepContext</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>DialogTurnResult</returns>
        private async Task<DialogTurnResult> GetCluIntent(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Use the CLU service
            try
            {
                Uri endpoint = new Uri("https://acnaclu.cognitiveservices.azure.com/");
                AzureKeyCredential credential = new AzureKeyCredential("7147a03774cb479cafd922b253ec26e3");

                ConversationAnalysisClient client = new ConversationAnalysisClient(endpoint, credential);

                string projectName = "AC-NA-CLU";
                string deploymentName = "Prueba";

                var data = new
                {
                    analysisInput = new
                    {
                        conversationItem = new
                        {
                            text = stepContext.Context.Activity.Text,
                            id = "1",
                            participantId = "user"
                        }
                    },
                    parameters = new
                    {
                        projectName,
                        deploymentName,
                        stringIndexType = "Utf16CodeUnit",
                    },
                    kind = "Conversation",
                };

                Response response = await client.AnalyzeConversationAsync(RequestContent.Create(data));

                using JsonDocument result = JsonDocument.Parse(response.ContentStream);
                JsonElement conversationalTaskResult = result.RootElement;
                JsonElement conversationPrediction = conversationalTaskResult.GetProperty("result").GetProperty("prediction");

                //The top intent
                var intent = conversationPrediction.GetProperty("topIntent").GetString();

                
                return await stepContext.NextAsync(intent, cancellationToken);
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Redirects to a specific dialog based on the user's input.
        /// </summary>
        /// <param name="stepContext">The context object that is passed to each step of a WaterfallDialog.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        private async Task<DialogTurnResult> Redirect(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the user's intent from the step context.
            string intent = stepContext.Result as string;

            // Use a switch statement to redirect to a specific dialog based on the user's intent.
            switch (intent)
            {
                case "ObtenerSaldoPresup":
                    return await stepContext.ReplaceDialogAsync(nameof(ObtenerSaldoPresupuestalDialog), cancellationToken: cancellationToken);
                case "ObtenerCeCo":
                    return await stepContext.ReplaceDialogAsync(nameof(TraspasoDialog), cancellationToken: cancellationToken);
                case "ObtenerSociedad":
                    return await stepContext.ReplaceDialogAsync(nameof(FAQsDialog), cancellationToken: cancellationToken);
                case "Saludo":
                    return await stepContext.ReplaceDialogAsync(nameof(SaludoDialog), cancellationToken: cancellationToken);
                    //return stepContext.EndDialogAsync();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
