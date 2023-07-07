using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace Bot.Api.Dialogs
{
    public class FAQsDialog : ComponentDialog
    {
        public FAQsDialog() : base(nameof(FAQsDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForSociedadAsync,
                DisplaySociedadAsync
            }));
        }

        private static async Task<DialogTurnResult> PromptForSociedadAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt the user to provide the sociedad name
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Por favor, dime el nombre de la sociedad:")
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private static async Task<DialogTurnResult> DisplaySociedadAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Display the sociedad name to the user
            var sociedad = stepContext.Result.ToString();
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"La sociedad que ingresaste es: {sociedad}"), cancellationToken);

            // End the dialog
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
