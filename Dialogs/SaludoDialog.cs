using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace Bot.Api.Dialogs
{
    public class SaludoDialog : ComponentDialog
    {
        public SaludoDialog() : base(nameof(SaludoDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SaludoAsync
            }));

        }

        private async Task<DialogTurnResult> SaludoAsync(WaterfallStepContext dialog, CancellationToken cancellationToken)
        {
            /*try
            {
                var user = await TeamsInfo.GetMemberAsync(dialog.Context, dialog.Context.Activity.Recipient.Id, cancellationToken);
                var name = user.Name;

                if (!name.EndsWith("(OFCORP)"))
                {
                    await dialog.Context.SendActivityAsync("No tiene acceso usted a este chatbot.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await dialog.Context.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
            }*/

            await dialog.Context.SendActivityAsync(MessageFactory.Text($"Hola! Estoy feliz por ayudarte, escribe tu pregunta o seleccionala del menu de abajo para que comencemos ;)"), cancellationToken);
            // Deploy a option's list to select or write the question
            await dialog.Context.SendActivityAsync(MessageFactory.Text($"Elije una opción o escribe tu pregunta"), cancellationToken);
            var card = new HeroCard
            {
                Text = "Aquí hay una variedad de opciones de las cuales puedes escoger:",
                Buttons = new List<CardAction>
                        {
                            new CardAction(ActionTypes.ImBack, title: "1) Consultar tu saldo presupuestal", value: "Consulta saldo presupuestal"),
                            new CardAction(ActionTypes.ImBack, title: "2) Solicitud de Traspaso", value: "Tengo una solicitud de traspaso"),
                            new CardAction(ActionTypes.ImBack, title: "3) FAQs (Preguntas frecuentes)", value: "Muestrame las preguntas frecuentes"),
                            new CardAction(ActionTypes.ImBack, title: "4) Solicitar Reporte Power BI", value: "Solicito un reporte Power BI")
                        }
            };
            await dialog.Context.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);
            await dialog.Context.SendActivityAsync(MessageFactory.Text($"O escribe tu propia pregunta =), solo está implementada la intención Consultar tu saldo presupuestal"), cancellationToken);
            return await dialog.EndDialogAsync(cancellationToken: cancellationToken);
        }

       
    }
}
