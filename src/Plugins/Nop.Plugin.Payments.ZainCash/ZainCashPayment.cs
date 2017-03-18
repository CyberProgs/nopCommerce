using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Payments;
using Nop.Core.Plugins;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Orders;
using System.Web;
using System.Web.Routing;
using Nop.Plugin.Payments.ZainCash.Controllers;


namespace Nop.Plugin.Payments.ZainCash
{

    
    //Credentials

    public class ZainCashPayment:BasePlugin,IPaymentMethod
    {

        private string merchantid = "57f3a0635a726a48ee912866";
        private string secret = "1zaza5a444a6e8723asd123asd123sfeasdase12312davwqf123123xc2ego";
        private double msisdn = 9647800624733;
        private string lang = "ar";  // "ar"   or    "en"
        private string service_type = "Testing... Put you service type here like: Books";
        private string redirection_url = "https://chanbar.com/testing.php"; //the url that will be redirected to, after completion of payment
        private int dollar_exchange_rate = 1300;




        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }



        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {


           
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }


        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            //var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
            //    _paypalStandardPaymentSettings.AdditionalFee, _paypalStandardPaymentSettings.AdditionalFeePercentage);
            //return result;
            return 0;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }


        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }


        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }


        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentZainCash";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.ZainCash.Controllers" }, { "area", null } };
        }


        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentZainCash";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.ZainCash.Controllers" }, { "area", null } };
        }


        public Type GetControllerType()
        {
            return typeof(PaymentZainCashController);
            
        }




        public bool SupportCapture
        {
            get { return false; }
        }


        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        public bool SupportRefund
        {
            get { return false; }
        }


        public bool SupportVoid
        {
            get { return false; }
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }


        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        public bool SkipPaymentInfo
        {
            get { return false; }
        }


        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
           // get { return _localizationService.GetResource("Plugins.Payments.PayPalStandard.PaymentMethodDescription"); }
            get { return ""; }
        }



        // zain payment 

        public string generate_zaincash_url(int orderid, float amount, bool isdollar)
        {
            //Change currency to dollar if required
            int new_amount;
            if (isdollar) { new_amount = (int)(amount * dollar_exchange_rate); } else { new_amount = (int)amount; }

            //Setting expiration of token
            Int32 iat = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Int32 exp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + 60 * 60 * 4;



            //Generate the data array
            IDictionary<string, object> dataarray = new Dictionary<string, object>();
            dataarray.Add("amount", new_amount);
            dataarray.Add("serviceType", service_type);
            dataarray.Add("msisdn", msisdn);
            dataarray.Add("orderId", orderid);
            dataarray.Add("redirectUrl", redirection_url);
            dataarray.Add("iat", iat);
            dataarray.Add("exp", exp);


            //Generating token
            string token = Jose.JWT.Encode(dataarray, System.Text.Encoding.ASCII.GetBytes(secret), Jose.JwsAlgorithm.HS256);


            //Posting token to ZainCash API to generate Transaction ID
            var httpclient = new System.Net.WebClient();
            var data_to_post = new System.Collections.Specialized.NameValueCollection();
            data_to_post["token"] = token;
            data_to_post["merchantId"] = merchantid;
            data_to_post["lang"] = lang;

            string response = System.Text.Encoding.ASCII.GetString(httpclient.UploadValues("https://api.zaincash.iq/transaction/init", "POST", data_to_post));

            //Parse JSON response to Object
            var jsona = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);

            //Return final URL
            return "https://api.zaincash.iq/transaction/pay?id=" + (string)jsona.id;
        }


        public Dictionary<string, string> after_redirection(string token)
        {
            //Convert token to json, then to object
            var jsona_res = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Jose.JWT.Decode(token, System.Text.Encoding.ASCII.GetBytes(secret)));

            //Generating response array
            Dictionary<string, string> final = new Dictionary<string, string>();
            final.Add("status", (string)jsona_res.status);
            if (jsona_res.status == "failed") { final.Add("msg", (string)jsona_res.msg); }

            return final;
        }

    // end zain payment
    }
}
