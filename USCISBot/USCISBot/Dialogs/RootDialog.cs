namespace USCISBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Helpers;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Payments;
    using Models;
    using Properties;
    using Services;
    using System.Security;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public const string CARTKEY = "CART_ID";

  
        public async Task StartAsync(IDialogContext context)
        {
            await Task.FromResult(true);
            context.Wait(this.MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument as Activity;
            await PrivacyPolicyAsync(context);
            User user = await GetUser(context, argument);
            Order order = StorageManager.Instance.Get(user.RowKey);
            var customerName = user.PartitionKey;
            bool IsSubscriped = order != null;
            string messageRecieved = activity.Text.ToLower();
            List<string> welcomeMessages = new List<string>() { "hi", "hello", "welcome", "dear" };
            var match = welcomeMessages.FirstOrDefault(stringToCheck => stringToCheck.Contains(messageRecieved));
            if (messageRecieved.Equals("privacy"))
            {
                await context.PostAsync($"Hi {customerName} kindly check our cosnumer terms and privacy policy \r\nif you continue to use our service you agree and bind to them");
                await context.PostAsync($"Privacy policy : http://uscisbot2017.azurewebsites.net/privacy/PrivacyPolicy.txt");
                await context.PostAsync($"Consumer Terms : http://uscisbot2017.azurewebsites.net/privacy/ConsumerTerms.txt");
            }
            else if (messageRecieved.Contains("help") || messageRecieved.Equals("?"))
            {
                await SendWelcomeMessage(context, customerName);
                await context.PostAsync($"simply type the following:");
                await context.PostAsync($"status for [name] cases [list of cases separated by ;] ");
                await context.PostAsync($"example : status for {customerName} cases 1234;1234");
                await context.PostAsync($"avilable commands : list , clear , status , help, privacy");

            }
            else if (messageRecieved.Equals("clear"))
            {
                user.Cases = null;
                StorageManager.Instance.Update(user);
                await context.PostAsync("cleared all tracking data !");

            }
            else if (messageRecieved.Equals("list"))
            {
                if (!string.IsNullOrWhiteSpace(user.Cases))
                {
                    string decrypt = AESCrypto.DecryptText(user.Cases, user.RowKey);
                    var dic = ToDictionary(decrypt);
                    foreach (var item in dic)
                    { 
                        await context.PostAsync(item.Key+" :");
                        string[] arr = item.Value.Split(new string[] {"$$$"},StringSplitOptions.RemoveEmptyEntries);
                        foreach (var r in arr)
                        {
                            await context.PostAsync(r);
                        }
                        
                    }
                    //don't show offer if subscriped
                    if (!IsSubscriped)
                    {
                        await this.PaymentMessageAsync(context, argument);
                    }
                }
                else
                {
                    await context.PostAsync("nothing there ! , type help for more details");
                }
            }
            else if (messageRecieved.Contains("status") || messageRecieved.Equals("check"))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(user.Cases))
                {
                    dic = ToDictionary(AESCrypto.DecryptText(user.Cases, user.RowKey));
                }

                List<string> statuses = new List<string>();
                if (messageRecieved.Equals("check"))
                {
                    statuses = new List<string>(dic.Keys);
                }
                else
                {
                    statuses.Add(messageRecieved);
                }
                foreach (var status in statuses)
                {

                    string[] arr = status.Split(new string[] { "cases" }, StringSplitOptions.RemoveEmptyEntries);
                    if (arr.Length < 2)
                    {
                        await context.PostAsync($"try this : status for {customerName} cases 1234;1234");
                        return;
                    }
                    string name = arr[0].Replace("status for", "").Trim();
                    string cases = arr[1].Trim();
                    List<string> casesList = cases.Split(';').ToList();

                    List<string> casesResultList = new List<string>();
                    string casesResult = string.Empty;

                    foreach (var c in casesList)
                    {
                        string uscisstatus, summary = string.Empty;
                        USCIS.UscisService.GetCaseStatus(c, out uscisstatus, out summary);
                        casesResult += $"{uscisstatus}$$${summary}$$$";
                        casesResultList.Add($"{uscisstatus}\r\n{summary}");
                    }

                    if (dic.ContainsKey(status))
                    {
                        string dicResult = dic[status];
                        if (casesResult.Equals(dicResult))
                        {
                            await context.PostAsync($"{status} \r\n No new updates for your cases , will keep an eye on it !");
                            continue;
                        }
                        else
                        {
                            dic[status] = casesResult;
                        }
                    }
                    else
                    {
                        dic.Add(status, casesResult);
                    }

                    user.Cases = AESCrypto.EncryptText(ToString(dic), user.RowKey);

                    // send result 
                    await context.PostAsync($"{name} status :");
                    foreach (var c in casesResultList)
                    {
                        await context.PostAsync(c);
                    }

                    await context.PostAsync($"I will keep an eye for {name} with cases {cases} and update you if there are any changes." + (IsSubscriped ? "" : " , if you buy our offer !"));

                    //don't show offer if subscriped
                    if (!IsSubscriped)
                    {
                        await this.PaymentMessageAsync(context, argument);
                    }

                    StorageManager.Instance.Update(user);
                }
            }
            else if (match != null)
            {
                await SendWelcomeMessage(context, customerName);
                await context.PostAsync($"what I can do for you today ? type help for more info.");
            }
            else// chat mode
            {
                string result = QnAService.GetAnswer(messageRecieved);
                if(result.Equals("No good match found in the KB"))
                {
                    result = "Still building my knowledge ask questions related to immigration , will troll with you once I became 1 year old";
                }

                await context.PostAsync(result);
            }
        }

        private async Task PrivacyPolicyAsync(IDialogContext context)
        {
            string customerName = context.Activity.From.Name;
            string customerId = context.Activity.From.Id;
            //await context.PostAsync($"name : {customerName} Id: {customerId}");
            User user = StorageManager.Instance.Get(customerName, customerId);
            if (user == null)
            {
                // show privacy policy
                await context.PostAsync($"Hi {customerName} kindly check our cosnumer terms and privacy policy \r\nif you continue to use our service you agree and bind to them");
                await context.PostAsync($"Privacy policy : http://uscisbot2017.azurewebsites.net/privacy/PrivacyPolicy.txt");
                await context.PostAsync($"Consumer Terms : http://uscisbot2017.azurewebsites.net/privacy/ConsumerTerms.txt");
            }
        }

        /// <summary>
        /// Returns a Secure string from the source string
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        private static SecureString ToSecureString(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;
            else
            {
                SecureString result = new SecureString();
                foreach (char c in source.ToCharArray())
                    result.AppendChar(c);
                return result;
            }
        }

        private static string ToString(Dictionary<string,string> map)
        {
            var result = string.Join("ا", map.Select(m => m.Key + "ب" + m.Value).ToArray());
            return result;
        }

        private static Dictionary<string, string> ToDictionary(string map)
        {
            var dic = map.Split('ا').Select(p => p.Trim().Split('ب')).ToDictionary(p => p[0], p => p[1]);
            return dic;

        }

        String SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        private static User AddOrGetUser(string customerPartitionKey, string customerRowKey)
        {
            User user = StorageManager.Instance.Get(customerPartitionKey, customerRowKey);
            if (user == null)
            {
                user = new User(customerPartitionKey, customerRowKey);
                StorageManager.Instance.Add(user);
            }

            return user;
        }

        private static async Task SendWelcomeMessage(IDialogContext context, string customerName)
        {
            await context.PostAsync($"Hello {customerName} , USCIS Non Official bot is here to help you !");
        }

        public async Task PaymentMessageAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var reply = context.MakeMessage();

            reply.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.RootDialog_Welcome_Msg,
                    context.Activity.From.Name);

            await context.PostAsync(reply);

            var replyMessage = context.MakeMessage();

            replyMessage.Attachments = new List<Attachment>();

            var catalogItem = await new CatalogService().GetRandomItemAsync();

            // store data for workarounds
            var cartId = catalogItem.Id.ToString();
            context.ConversationData.SetValue(CARTKEY, cartId);
            context.ConversationData.SetValue(cartId, context.Activity.From.Id);

            replyMessage.Attachments.Add(await BuildBuyCardAsync(cartId, catalogItem));

            await context.PostAsync(replyMessage);

            context.Wait(this.AfterPurchaseAsync);
        }

        private static PaymentRequest BuildPaymentRequest(string cartId, CatalogItem item, MicrosoftPayMethodData methodData)
        {
            return new PaymentRequest
            {
                Id = cartId,
                Expires = TimeSpan.FromDays(1).ToString(),
                MethodData = new List<PaymentMethodData>
                {
                    methodData.ToPaymentMethodData()
                },
                Details = new PaymentDetails
                {
                    Total = new PaymentItem
                    {
                        Label = Resources.Wallet_Label_Total,
                        Amount = new PaymentCurrencyAmount
                        {
                            Currency = item.Currency,
                            Value = Convert.ToString(item.Price, CultureInfo.InvariantCulture)
                        },
                        Pending = true
                    },
                    DisplayItems = new List<PaymentItem>
                    {
                        new PaymentItem
                        {
                            Label = item.Title,
                            Amount = new PaymentCurrencyAmount
                            {
                                Currency = item.Currency,
                                Value = item.Price.ToString(CultureInfo.InvariantCulture)
                            }
                        },
                        new PaymentItem
                        {
                            Label = Resources.Wallet_Label_Shipping,
                            Amount = new PaymentCurrencyAmount
                            {
                                Currency = item.Currency,
                                Value = "0.00"
                            },
                            Pending = true
                        },
                        new PaymentItem
                        {
                            Label = Resources.Wallet_Label_Tax,
                            Amount = new PaymentCurrencyAmount
                            {
                                Currency = item.Currency,
                                Value = "0.00"
                            },
                            Pending = true
                        }
                    }
                },
                Options = new PaymentOptions
                {
                    RequestShipping = true,
                    RequestPayerEmail = true,
                    RequestPayerName = true,
                    RequestPayerPhone = true,
                    ShippingType = PaymentShippingTypes.Shipping
                }
            };
        }

        private static Task<Attachment> BuildBuyCardAsync(string cartId, CatalogItem item)
        {
            var heroCard = new HeroCard
            {
                Title = item.Title,
                Subtitle = $"{item.Currency} {item.Price.ToString("F")}",
                Text = item.Description,
                Images = new List<CardImage>
                {
                    new CardImage
                    {
                        Url = item.ImageUrl
                    }
                },
                Buttons = new List<CardAction>
                {
                    new CardAction
                    {
                        Title = "Buy",
                        Type = PaymentRequest.PaymentActionType,
                        Value = BuildPaymentRequest(cartId, item, PaymentService.GetAllowedPaymentMethods())
                    }
                }
            };

            return Task.FromResult(heroCard.ToAttachment());
        }

        private static ReceiptItem BuildReceiptItem(string title, string subtitle, string price, string imageUrl)
        {
            return new ReceiptItem(
                title: title,
                subtitle: subtitle,
                price: price,
                image: new CardImage(imageUrl));
        }

        private static async Task<Attachment> BuildReceiptCardAsync(PaymentRecord paymentRecord , string userId)
        {
            var shippingOption = await new ShippingService().GetShippingOptionAsync(paymentRecord.ShippingOption);

            var catalogItem = await new CatalogService().GetItemByIdAsync(paymentRecord.OrderId);

            var receiptItems = new List<ReceiptItem>();

            receiptItems.AddRange(paymentRecord.Items.Select<PaymentItem, ReceiptItem>(item =>
            {
                if (catalogItem.Title.Equals(item.Label))
                {
                    return RootDialog.BuildReceiptItem(
                        catalogItem.Title,
                        catalogItem.Description,
                        $"{catalogItem.Currency} {catalogItem.Price.ToString("F")}",
                        catalogItem.ImageUrl);
                }
                else
                {
                    return RootDialog.BuildReceiptItem(
                        item.Label,
                        null,
                        $"{item.Amount.Currency} {item.Amount.Value}",
                        null);
                }
            }));

            var receiptCard = new ReceiptCard
            {
                Title = Resources.RootDialog_Receipt_Title,
                Facts = new List<Fact>
                {
                    new Fact(Resources.RootDialog_Receipt_OrderID, paymentRecord.OrderId.ToString()),
                    new Fact(Resources.RootDialog_Receipt_PaymentMethod, paymentRecord.MethodName),
                    new Fact(Resources.RootDialog_Shipping_Address, paymentRecord.ShippingAddress.FullInline()),
                    new Fact(Resources.RootDialog_Shipping_Option, shippingOption != null ? shippingOption.Label : "N/A")
                },
                Items = receiptItems,
                Tax = null, // Sales Tax is a displayed line item, leave this blank
                Total = $"{paymentRecord.Total.Amount.Currency} {paymentRecord.Total.Amount.Value}"
            };

            AddOrder(paymentRecord, userId);
            return receiptCard.ToAttachment();
        }

        private static void AddOrder(PaymentRecord paymentRecord, string userId)
        {
            Order order = new Order(userId);
            order.ShippingAddress = paymentRecord.ShippingAddress.FullInline();
            order.ShippingOption = paymentRecord.ShippingOption;
            order.Title = paymentRecord.Items.FirstOrDefault().Label;
            order.Total = Convert.ToDecimal(paymentRecord.Total.Amount.Value);
            order.OrderId = paymentRecord.OrderId.ToString();
            order.TransactionId = paymentRecord.TransactionId.ToString();
            StorageManager.Instance.Add(order);
        }

        private async Task AfterPurchaseAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            // clean up state store after completion
            var cartId = context.ConversationData.Get<string>(CARTKEY);
            context.ConversationData.RemoveValue(CARTKEY);
            context.ConversationData.RemoveValue(cartId);

            var activity = await argument as Activity;
            var paymentRecord = activity?.Value as PaymentRecord;

            if (paymentRecord == null)
            {
                // show error
                var errorMessage = activity.Text;
                var message = context.MakeMessage();
                message.Text = errorMessage;

                await this.StartOverAsync(context, argument, message);
            }
            else
            {
                // show receipt
                var message = context.MakeMessage();
                message.Text = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.RootDialog_Receipt_Text,
                    paymentRecord.OrderId,
                    paymentRecord.PaymentProcessor);

                message.Attachments.Add(await BuildReceiptCardAsync(paymentRecord, context.Activity.From.Id));

                await this.StartOverAsync(context, argument, message);
            }
        }

        private static async Task<User> GetUser(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument as Activity;
            string customerName = context.Activity.From.Name;
            string customerId = context.Activity.From.Id;
            //await context.PostAsync($"name : {customerName} Id: {customerId}");
            User user = AddOrGetUser(customerName, customerId);
            return user;
        }

        private async Task StartOverAsync(IDialogContext context, IAwaitable<IMessageActivity> argument, IMessageActivity message)
        {
            await context.PostAsync(message);

            context.Wait(this.MessageReceivedAsync);
        }
    }
}