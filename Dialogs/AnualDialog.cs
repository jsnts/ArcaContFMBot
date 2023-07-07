using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using System;
using Bot.Api.Data;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using AdaptiveCards;
using System.Collections.Generic;
using System.Linq;
using static Azure.Core.HttpHeader;

namespace Bot.Api.Dialogs
{
    public class AnualDialog : ComponentDialog
    {
        public AnualDialog() : base(nameof(AnualDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForSociety,
                PrintCeCosUsuario,
                PromptForCeCo,
                PrintCuentasUsuario,
                PromptForNumCuenta,
                ShowUsers,
                End
            }));
        }

        private async Task<DialogTurnResult> ReadInfo(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.ReplaceDialogAsync(nameof(ReadInfoDialog), cancellationToken: cancellationToken);
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
                // Ejecutamos la conexión
                var reader = db.ExecuteReader(query);

                var adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
                var container = new AdaptiveContainer();

                var cardTextBlock = new AdaptiveTextBlock
                {
                    Text = "Elige entre las siguientes cuentas relacionadas con tus Centros de Costo seleccionados:",
                    Size = AdaptiveTextSize.Default,
                    Weight = AdaptiveTextWeight.Default,
                    Wrap = true
                };
                container.Items.Add(cardTextBlock);

                var actions = new List<AdaptiveAction>();

                var cuentaList = new List<string>();

                while (reader.Read())
                {
                    var NumCuenta = reader["Pos_Pre"].ToString();
                    var DescCuenta = reader["Desc_PosPre"].ToString();
                    var Sociedad = society;

                    var buttonAction = new AdaptiveSubmitAction
                    {
                        Title = DescCuenta,
                        Data = NumCuenta
                    };
                    actions.Add(buttonAction);

                    cuentaList.Add(NumCuenta);
                }

                stepContext.Values["AUX"] = cuentaList;

                actions.Add(new AdaptiveSubmitAction
                {
                    Title = "Todas las cuentas",
                    Data = "Todas"
                });

                adaptiveCard.Body.Add(container);
                adaptiveCard.Actions.AddRange(actions.Select(a => a));

                var attachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = adaptiveCard
                };

                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);

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
                Prompt = MessageFactory.Text("Introduce el CeCo")
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> PrintCuentasUsuario(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if((string)stepContext.Result == "Todos")
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

        private async Task<DialogTurnResult> ShowUsers(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result == "Todas")
            {
                stepContext.Values["Cuenta"] = stepContext.Values["AUX"];
            }
            else
            {
                stepContext.Values["Cuenta"] = new List<string>()
                {
                    (string)stepContext.Result
                };
            }

            //Creamos la instancia para la conexion 
            var db = new DatabaseService("sqlserverdac.database.windows.net", "databaseac", "usrteam1", "XW9ZEzoa");

            // Retrieve the saved parameters from the stepContext.Values dictionary
            var society = (string)stepContext.Values["Society"];
            var ceco = (List<string>)stepContext.Values["CeCo"];
            var numCuenta = (List<string>)stepContext.Values["Cuenta"];

            var tableName = society switch
            {
                "DAC" => "[DummyDAC]",
                "AC SAB" => "[Dummy_AC_SAB]",
                "SAB" => "[Dummy_AC_SAB]",
                "AC_SAB" => "[Dummy_AC_SAB]",
                _ => throw new ArgumentException($"Invalid society: {society}")
            };

            // Construct the query using the CeCo and NumCuenta parameters
            var query = $@"SELECT Desc_CeGe, Centro_Gestor, Desc_PosPre, Pos_Pre, PPTO_Anual
                   FROM {tableName}
                   WHERE Centro_Gestor IN ({string.Join(",", ceco.Select(c => $"'{c}'"))})
                   AND Pos_Pre IN({ string.Join(",", numCuenta.Select(c => $"'{c}'"))})";

            try
            {
                //Ejecutamos la conexion
                var reader = db.ExecuteReader(query);

                var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
                var columnSet = new AdaptiveColumnSet();
                
                var column1 = new AdaptiveColumn() { Width = "25%" };
                var column2 = new AdaptiveColumn() { Width = "25%" };
                var column3 = new AdaptiveColumn() { Width = "25%" };
                var column4 = new AdaptiveColumn() { Width = "25%" };

                column1.Items.Add(new AdaptiveTextBlock() { Text = "CeCo", Wrap = true });
                column2.Items.Add(new AdaptiveTextBlock() { Text = "Cuenta", Wrap = true  });
                column3.Items.Add(new AdaptiveTextBlock() { Text = "Sociedad"});
                column4.Items.Add(new AdaptiveTextBlock() { Text = "Saldo (MX)", Wrap = true });

                while (reader.Read())
                {
                    var CeCo = reader["Centro_Gestor"].ToString();
                    var NumCuenta = reader["Pos_Pre"].ToString();
                    var sociedad = society;
                    var saldoPr = Convert.ToDecimal(reader["PPTO_Anual"]);
                    var saldoPresupuestal = "$" + saldoPr.ToString("N2");

                    column1.Items.Add(new AdaptiveTextBlock() { Text = CeCo, Wrap = true, Height = AdaptiveHeight.Stretch });
                    column2.Items.Add(new AdaptiveTextBlock() { Text = NumCuenta, Wrap = true, Height = AdaptiveHeight.Stretch });
                    column3.Items.Add(new AdaptiveTextBlock() { Text = sociedad, Wrap = true, Height = AdaptiveHeight.Stretch });
                    column4.Items.Add(new AdaptiveTextBlock() { Text = saldoPresupuestal, Wrap = true, Height = AdaptiveHeight.Stretch });
                }

                //columnSet.Columns.Add(columnCeCo);
                columnSet.Columns.Add(column1);
                //columnSet.Columns.Add(columnCuenta);
                columnSet.Columns.Add(column2);
                columnSet.Columns.Add(column3);
                columnSet.Columns.Add(column4);

                card.Body.Add(columnSet);

                Attachment attachment = new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                };

                var reply = MessageFactory.Attachment(attachment);
                await stepContext.Context.SendActivityAsync(reply);

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

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.NextAsync(cancellationToken: cancellationToken);
        }
    }
}
