// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Bot.Api.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.Metrics;

namespace Bot.Api
{
    public class Bot : ActivityHandler
    {
        private readonly ConversationState _conversationState;
        private readonly DialogSet _dialogSet;
        private readonly BotState _userState;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bot"/> class.
        /// </summary>
        /// <param name="userState">The user state.</param>
        /// <param name="conversationState">The conversation state.</param>
        public Bot(UserState userState, ConversationState conversationState)
        {
            _userState = userState;
            _conversationState = conversationState;
            _dialogSet = new DialogSet(conversationState.CreateProperty<DialogState>(nameof(DialogState)));

            // Adding the dialogs to the DialogSet.
            _dialogSet.Add(new RootDialog(_conversationState));
            _dialogSet.Add(new ObtenerSaldoPresupuestalDialog());
            _dialogSet.Add(new TraspasoDialog());
            _dialogSet.Add(new FAQsDialog());
            _dialogSet.Add(new SaludoDialog());
            _dialogSet.Add(new DisponibleAlMomentoDialog());
            _dialogSet.Add(new AnualDialog());
            _dialogSet.Add(new ComprometidoDialog());
            _dialogSet.Add(new GastadoTotalDialog());
            _dialogSet.Add(new ImporteLiberadoDialog());
            _dialogSet.Add(new RealDevengadoDialog());
            _dialogSet.Add(new ReadInfoDialog());
            _dialogSet.Add(new TextPrompt(nameof(TextPrompt)));
        }


        /// <summary>
        /// Called by the adapter at runtime to process an incoming activity, outgoing activity, or an update to a conversation's state.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _conversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Welcome the user using their first name
                    try
                    {
                        var user = await TeamsInfo.GetMemberAsync(turnContext, member.Id, cancellationToken);
                        var name = user.Name;
                        string[] parts = name.Split(new char[] { ' ' });
                        string firstName = parts[2];
                        firstName = firstName.Substring(0, 1).ToUpper() + firstName.Substring(1).ToLower();
                        var LastName = parts[0].Substring(0, 1).ToUpper() + parts[0].Substring(1).ToLower()
                                 + parts[1].Substring(0, 1).ToUpper() + parts[1].Substring(1).ToLower();
                        var secondName = parts[3];

                        if (!name.EndsWith("(OFCORP)"))
                        {
                            await turnContext.SendActivityAsync("No tiene acceso usted a este chatbot.");
                            int x = 0;
                            while (true)
                            {
                                x++;
                            }
                            return;
                        }

                        await turnContext.SendActivityAsync(MessageFactory.Text($"<b>Bienvenido al Chatbot Presupuestal {firstName}!</b>"), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await turnContext.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
                    }

                    // Deploy a option's list to select or write the question
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Elije una opción o escribe tu pregunta"), cancellationToken);
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
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text($"O escribe tu propia pregunta =), solo está implementada la intención Consultar tu saldo presupuestal"), cancellationToken);

                    // Verify the user authentication

                }
            }
        }

        /// <summary>
        /// Overrides the OnMessageActivityAsync method to handle incoming messages.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// 
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var dialogContext = await _dialogSet.CreateContextAsync(turnContext, cancellationToken);

            // Use the DialogSet to start the dialog if it hasn't started yet.
            var result = await dialogContext.ContinueDialogAsync(cancellationToken);

            if (result.Status == DialogTurnStatus.Empty)
            {
                await dialogContext.BeginDialogAsync(typeof(RootDialog).Name, null, cancellationToken);
            }

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
    }
}