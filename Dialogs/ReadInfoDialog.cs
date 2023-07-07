using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using System;
using Bot.Api.Data;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Teams;
using System.Net.Sockets;
using System.Linq;

namespace Bot.Api.Dialogs
{
    public class ReadInfoDialog : ComponentDialog
    {
        public ReadInfoDialog() : base(nameof(ReadInfoDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForSociety,
                PrintCeCosUsuario,
                PromptForCeCo,
                PrintCuentasUsuario,
                PromptForNumCuenta,
                End
            }));
        }

        private async Task<DialogTurnResult> PromptForSociety(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Text = "Aquí hay una variedad de opciones de las cuales puedes escoger:",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, title: "1) AC SAB", value: "AC_SAB"),
                    new CardAction(ActionTypes.ImBack, title: "2) DAC", value: "DAC")
                }
            };
            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Por favor ingresa la sociedad a la que perteneces.")
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> PrintCeCosUsuario(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Society"] = (string)stepContext.Result;

            //Creamos la instancia para la conexion 
            var db = new DatabaseService("sqlserverdac.database.windows.net", "databaseac", "usrteam1", "XW9ZEzoa");

            var table = "users";
            var name = "Angel Manuel Tapia Avitia";
            var sociedad = stepContext.Values["Society"];

            string query = $@"SELECT CeCo, DescCeCo, Sociedad
                   FROM {table}
                   WHERE Name = '{name}' 
                   AND Sociedad = '{sociedad}'";

            try
            {
                //Ejecutamos la conexion
                var reader = db.ExecuteReader(query);
                var cardC = new HeroCard()
                {
                    Text = "Escoje de estos centros de costos disponibles para tu usuario:",
                    Buttons = new List<CardAction>()
                };

                var cecoList = new List<string>();

                while (reader.Read())
                {
                    var CeCo = reader["CeCo"].ToString();
                    var DescCeCo = reader["DescCeCo"].ToString();
                    var Sociedad = reader["Sociedad"].ToString();
                    //await stepContext.Context.SendActivityAsync($"Centro de Costos: {CeCo}, Numero de cuenta: {NumCuenta}, Sociedad: {sociedad}, Saldo Presupuestal: {saldoPresupuestal}");

                    cardC.Buttons.Add(new CardAction(ActionTypes.ImBack, title: $@"{CeCo} {DescCeCo}", value: $@"{CeCo}"));
                    cecoList.Add(CeCo);
                }

                stepContext.Values["AUX"] = cecoList;

                cardC.Buttons.Add(new CardAction(ActionTypes.ImBack, title: $@"Todos los Centros de Costos", value: $@"Todos"));
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(cardC.ToAttachment()), cancellationToken);

                reader.Close();
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("The bot encountered an error or bug.", cancellationToken: cancellationToken);
                await stepContext.Context.SendActivityAsync("To continue to run this bot, please fix the bot source code.", cancellationToken: cancellationToken);
                await stepContext.Context.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
            }

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForCeCo(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Por favor ingresa el CeCo que quieres consultar.")
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> PrintCuentasUsuario(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result == "Todos")
            {
                stepContext.Values["CeCo"] = stepContext.Values["AUX"];
            }
            else
            {
                stepContext.Values["CeCo"] = new List<string>()
                {
                    (string)stepContext.Result
                };
            }

            //Creamos la instancia para la conexion 
            var db = new DatabaseService("sqlserverdac.database.windows.net", "databaseac", "usrteam1", "XW9ZEzoa");

            var society = (string)stepContext.Values["Society"];
            var ceco = (List<string>)stepContext.Values["CeCo"];
            var table = society switch
            {
                "DAC" => "[DummyDAC]",
                "AC SAB" => "[Dummy_AC_SAB]",
                "SAB" => "[Dummy_AC_SAB]",
                "AC_SAB" => "[Dummy_AC_SAB]",
                _ => throw new ArgumentException($"Invalid society: {society}")
            };
            var name = "Angel Manuel Tapia Avitia";

            string query = $@"SELECT Desc_PosPre, Pos_Pre
                               FROM {table}
                               WHERE Centro_Gestor IN ({string.Join(",", ceco.Select(c => $"'{c}'"))})";

            try
            {
                //Ejecutamos la conexion
                var reader = db.ExecuteReader(query);

                var cardC = new HeroCard()
                {
                    Text = "Escoje de estas cuentas relacionadas a tu o tus Centros de Costo previamente elegidos:",
                    Buttons = new List<CardAction>()
                };

                var cuentaList = new List<string>();

                while (reader.Read())
                {
                    var NumCuenta = reader["Pos_Pre"].ToString();
                    var DescCuenta = reader["Desc_PosPre"].ToString();
                    var Sociedad = society;

                    cardC.Buttons.Add(new CardAction(ActionTypes.ImBack, title: $@"{DescCuenta}", value: $@"{NumCuenta}"));

                    cuentaList.Add(NumCuenta);
                }
                stepContext.Values["AUX"] = cuentaList;

                cardC.Buttons.Add(new CardAction(ActionTypes.ImBack, title: $@"Todas las cuentas", value: $@"Todas"));
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(cardC.ToAttachment()), cancellationToken);

                reader.Close();
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("The bot encountered an error or bug.", cancellationToken: cancellationToken);
                await stepContext.Context.SendActivityAsync("To continue to run this bot, please fix the bot source code.", cancellationToken: cancellationToken);
                await stepContext.Context.SendActivityAsync(ex.Message, cancellationToken: cancellationToken);
            }


            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForNumCuenta(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Por favor ingresa el numero de cuenta que quieres consultar.")
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["NumCuenta"] = (string)stepContext.Result;

            return await stepContext.NextAsync(stepContext, cancellationToken: cancellationToken);
        }
    }
}
