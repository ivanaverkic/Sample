using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Sample
{
    public partial class MainWindow : Form
    {
        #region Fields

        private string currencyCodeUrl = "http://currencysystem.com/codes/";
        private string currencyConvertUrl = "https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={0}&to_currency={1}&apikey=UMJLYDH29L30PFUJ";
        private string fromCurrency;
        private string toCurrency;
        private string stringResponse;

        private readonly List<string> exchangeRateResult = new List<string>();
        private readonly List<string> currencyNodes = new List<string>();

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Load

        private void MainWindow_Load(object sender, EventArgs e)
        {
            GetRequest(currencyCodeUrl);
            GetCountryAndCode();
            FillComboBoxes();
        }

        #endregion

        #region EventHandlers

        private void FromComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            fromCurrency = FromComboBox.SelectedItem.ToString();
            fromCurrency = new string(fromCurrency.Take(3).ToArray());
        }

        private void ToComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            toCurrency = ToComboBox.SelectedItem.ToString();
            toCurrency = new string(toCurrency.Take(3).ToArray());
        }

        private void Convert_Click(object sender, EventArgs e)
        {
            try
            {
                var url = string.Format(currencyConvertUrl, fromCurrency, toCurrency);
                GetRequest(url);
                dataGridView1.DataSource = JsonToDataTable();
                dataGridView1.AutoResizeColumns();
                dataGridView1.AutoResizeRows();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Exception {0} was thrown.", ex.ToString());
            }
        }
        #endregion

        #region Methods

        private void GetRequest(string url)
        {
            try
            {
                var http = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)http.GetResponse();
                var enc = Encoding.GetEncoding(1252);
                var responseStream = new StreamReader(response.GetResponseStream(), enc);

                stringResponse = responseStream.ReadToEnd();

                response.Close();
                responseStream.Close();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Exception {0} was thrown.", ex.ToString());
            }
        }

        private void GetCountryAndCode()
        {
            try
            {
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(stringResponse);

                HtmlNode docNode = doc.DocumentNode;
                HtmlNodeCollection trNodes = docNode.SelectNodes("//tr");

                //Get all values that are in <td> tags
                foreach (HtmlNode trNode in trNodes)
                {
                    HtmlNodeCollection tdNodes = trNode.SelectNodes("td");
                    if (tdNodes == null)
                    {
                        continue;
                    }
                    GetCurrencyNodes(tdNodes);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Exception {0} was thrown.", ex.ToString());
            }
        }

        private void GetCurrencyNodes(HtmlNodeCollection tdNodes)
        {
            foreach (var td in tdNodes)
            {
                //Fill list with each <td> tag containing county name or country code
                if (!td.InnerText.Contains("\r\n") && !td.InnerText.Contains("&nbsp;"))
                {
                    currencyNodes.Add(td.InnerText);
                }
            }
        }

        public void FillComboBoxes()
        {
            //Add each country code and country name as one element in list.
            int j = 1;
            for (int i = 0; i < currencyNodes.Count; i += 2)
            {
                exchangeRateResult.Add(currencyNodes[j] + " (" + currencyNodes[i] + ")");
                j += 2;
            }

            foreach (string currency in exchangeRateResult)
            {
                FromComboBox.Items.Add(currency);
                ToComboBox.Items.Add(currency);
            }
        }

        private DataTable JsonToDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add();
            dt.Columns.Add();

            var currencyNameByAbbrv = new Dictionary<string, JToken>();

            try
            {
                //Show an error message to the user if the site does not do the specific conversion selected. 
                if (stringResponse.Contains("Error Message"))
                {
                    MessageBox.Show("Invalid API call. The site cannot convert these currencies, please try again with different currencies",
                        "Currency Convert Error Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return dt;
                }

                //dynamic json = JsonConvert.DeserializeObject(response);
                var outer = JToken.Parse(stringResponse);
                var inner = outer["Realtime Currency Exchange Rate"].Value<JObject>();

                //Fill dictionary from json text
                foreach (var currency in inner.Properties().Select(p => p))
                {
                    currencyNameByAbbrv.Add(currency.Name, currency.Value);
                }

                foreach (var nameAndAbbrv in currencyNameByAbbrv)
                {
                    string currencyName = Regex.Replace(nameAndAbbrv.Key, @"[^a-zA-Z\s]+", "", RegexOptions.Compiled);
                    dt.Rows.Add(currencyName, nameAndAbbrv.Value);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Exception {0} was thrown.", ex.ToString());
            }
            return dt;
        }

        #endregion

    }
}
