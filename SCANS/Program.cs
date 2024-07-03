using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Data;
using System.Net;
using ZEE.DAL;
using Mail;

namespace SCANS
{
    class Program
    {
        static string ConnStrDBDisplayName = System.Configuration.ConfigurationManager.AppSettings["SqlDbDisplayName"].ToString();
        static string ConnStr = System.Configuration.ConfigurationManager.AppSettings["SqlDbCredential"].ToString();


        static string TimerFrequency = System.Configuration.ConfigurationManager.AppSettings["TimerFrequency"].ToString();

        public static Thread TCheckSP = null;
        public static Thread CheckCommodity = null;
        public static bool CheckDbFlag = true;
        static void Main(string[] args)
        {

            KillProcess();
            try
            {
                TCheckSP.Abort();
            }
            catch (Exception ex1) { Logger.writeInLogFile("Error : " + ex1.Message.ToString() + " at line no :" + (new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber().ToString()); }

            TCheckSP = new Thread(SubCheckSP);
            TCheckSP.Start();

        }

        static void SubCheckSP()
        {
            while (CheckDbFlag)
            {

                try
                {
                    Console.Title = "SCANS (" + ConnStrDBDisplayName + ") ";


                    TimeSpan startTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Open"].ToString());
                    TimeSpan endTimeTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Close"].ToString());
                    TimeSpan CurrTime = TimeSpan.Parse(DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":00");

                    if (CurrTime >= startTime && CurrTime <= endTimeTime)
                    {
                        CheckSPs(); // check on Main DB
                        Console.WriteLine(DateTime.Now);

                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now +  " Market not open yet.");
                    }
                    Thread.Sleep(1000 * Convert.ToInt32(TimerFrequency));



                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + " " + ex.Message);
                }

            }
        }
        private static void CheckSPs()
        {

            try
            {

                //using (WebClient client = new WebClient())
                //{
                //string s;
                //ServicePointManager.Expect100Continue = true;
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //s =  client.DownloadString(");

                //                   ServicePointManager.Expect100Continue = true;
                //                   ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                //                          | SecurityProtocolType.Tls11
                //                          | SecurityProtocolType.Tls12
                //                          | SecurityProtocolType.Ssl3;

                //                   HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.nseindia.com/companies-listing/corporate-filings-directory?symbol=YESBANK&tabIndex=equity");
                ////string s;
                //               }


                SP_SCANS_VOL_SHOCKERS(10, "GAINER");
                SP_SCANS_VOL_SHOCKERS(10, "LOSER");
                SP_SCAN_BUZZING_STOCKS(1, 2, 1, "GAINER","PRICE");
                SP_SCAN_BUZZING_STOCKS(1, 2, 1, "LOSER","PRICE");
                SP_SCANS_OFF_7_DAYS_LOW(1);

                string strSql = "select STOCK_ID,EXCHANGE_SYMBOL, ABBRs  from STOCK_ITEMS where SECTOR_ID  like ('%,1,%')";
                DataTable dtStkId = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, strSql).Tables[0];

                //for (int i = 0; i < dtStkId.Rows.Count; i++)
                //{

                //    SP_SCAN_GET_TICKVOLUME_SPIKE(dtStkId.Rows[i]["STOCK_ID"].ToString(), dtStkId.Rows[i]["EXCHANGE_SYMBOL"].ToString(), dtStkId.Rows[i]["ABBRs"].ToString());
                 
                //}

            }

            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR IN exec SP_SCANS_VOL_SHOCKERS: " + ex.Message);
            }
        }


        private static void SP_SCAN_BUZZING_STOCKS(int strEXchangeId, int strDurationinMins, int strSectorId, string strVariant, string strType)
        {
            Logger.writeInLogFile("getStockUpdate() " + " DB " + ConnStrDBDisplayName);
            string sql = "";

            sql = "exec SP_SCAN_BUZZING_STOCKS " +  strEXchangeId + "," +  strDurationinMins+ "," +  strSectorId +",'" + strVariant + "','" + strType + "'";


            bool isFound = false;
            try
            {
                // DateTime dtStockUpdateTime, dtTickTime;
                DataTable dtStkId = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, sql).Tables[0];
                Dictionary<string, string> stkList = new Dictionary<string, string>();
                if (dtStkId.Rows.Count > 1)
                {
                    //Console.WriteLine("DB : " + ConnStrDBDisplayName + " - Status : Healthy" + DateTime.Now.ToString());
                    //Logger.writeInLogFile("DB : " + ConnStrDBDisplayName + " - Status : Healthy");

                    for (int i = 0; i <= dtStkId.Rows.Count-1; i++)
                    {
                        try
                        {

                            string strSql = "select * from scans where sp_name ='" + sql.Replace("'", "") + "' and stock_id ='" + dtStkId.Rows[i]["stock_id"].ToString() + "'";


                            DataTable dtscans = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, strSql).Tables[0];
                            if (dtscans.Rows.Count == 0)
                            {
                                strSql = "insert into scans (SP_NAME,STOCK_ID) values ('" + sql.Replace("'", "") + "','" + dtStkId.Rows[i]["stock_id"].ToString() + "')";
                                SqlHelper.ExecuteNonQuery(ConnStr, CommandType.Text, strSql);
                                //   stkList.Add(dtStkId.Rows[i]["abbrs"].ToString() + "::" + dtStkId.Rows[i]["exchange_symbol"].ToString(), dtStkId.Rows[i]["abbrs"].ToString() + "~" + dtStkId.Rows[i]["exchange_symbol"].ToString() + "~" + dtStkId.Rows[i]["acc_volume"].ToString() + "~" + dtStkId.Rows[i]["avg_volume"].ToString() + "~" + dtStkId.Rows[i]["volume change (Times)"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i]["perc_change"].ToString() + "~" + dtStkId.Rows[i]["UPDATE_DATE_TIME"].ToString());
                                stkList.Add(dtStkId.Rows[i]["stock_id"].ToString() + "::" + dtStkId.Rows[i]["update_date_time"].ToString(), dtStkId.Rows[i]["abbrs"].ToString() + "~" + dtStkId.Rows[i]["exchange_symbol"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i][+strDurationinMins + "mins_lastprice"].ToString() + "~" + dtStkId.Rows[i]["duration_price_perc_change"].ToString() + "~" + dtStkId.Rows[i]["duration_acc_vol_perc_change"].ToString() + "~" + dtStkId.Rows[i]["perc_change"].ToString());
                                Console.WriteLine("SP_SCAN_BUZZING_STOCKS " + dtStkId.Rows[i]["stock_id"].ToString() + " " + dtStkId.Rows[i]["abbrs"].ToString());
                                isFound = true;
                            }
                               
                        }
                        catch (Exception ex)
                        {
                            //string msg = dtStkId.Rows[i]["name"].ToString() + "~" + dtStkId.Rows[i]["code"].ToString();
                        }
                    }
                    TimeSpan startTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Open"].ToString());
                    TimeSpan endTimeTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Close"].ToString());
                    TimeSpan CurrTime = TimeSpan.Parse(DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":00");


                        if (stkList.Count > 0)
                        {
                            string subject = "";
                          

                        if (strVariant == "GAINER")
                        {
                            subject= " PRICE SPIKE  (NIFTY 500)<br/><br/>";
                        }
                        else
                        {
                            subject= " PRICE DIP (NIFTY 500)<br/><br/>";
                        }

                        string to = System.Configuration.ConfigurationManager.AppSettings["toId"].ToString();
                            SendMail sm = new SendMail();



                            string mailBody = "";
                        if (strVariant == "GAINER")
                        {
                            mailBody = mailBody + " PRICE SPIKE  (NIFTY 500)<br/><br/>";
                        }
                        else
                        {
                            mailBody = mailBody + " PRICE DIP (NIFTY 500)<br/><br/>";
                        }
                        

                            mailBody = mailBody + "<table style=\"width:100%;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<tr style=\"align:center;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<th>Stock Name</th>";
                            mailBody = mailBody + "<th>Symbol</th>";
                            mailBody = mailBody + "<th>CMP</th>";
                            mailBody = mailBody + "<th>CMP -" + strDurationinMins+" mins ago</th>";
                            mailBody = mailBody + "<th>% Jump in Price</th>";
                            mailBody = mailBody + "<th>% Jump in Volume</th>";
                             mailBody = mailBody + "<th>%</th>";
                            mailBody = mailBody + "</tr>";

                            foreach (var item in stkList)
                            {
                                mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                // mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[6].ToString().Replace("'", "''") + "</td>";

                                //    mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + " %</td>";
                                //    Console.WriteLine("LOSER : " + item.Value.Split('~')[0].ToString().Replace("'", "''") + ". " + DateTime.Now.ToString());


                                //mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";

                                mailBody = mailBody + "</tr>";

                            }

                            mailBody = mailBody + "</table><br/><br/>";
                            mailBody = mailBody + " Thanks & Regards" + "<br/>";
                            mailBody = mailBody + " Tech Team, Mumbai" + "<br/>";
                            try
                            {
                                try
                                {
                                    string s = sm.Mail(to, subject, mailBody);
                                }
                                catch (Exception ex)
                                {

                                    // throw;
                                }



                                foreach (var item in stkList)
                                {
                                    mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                    mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[6].ToString().Replace("'", "''") + "</td>";
                                    //mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + "</td>";
                                    //mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";


                                    mailBody = mailBody + "</tr>";

                                    //Console.WriteLine(item.Value.Split('~')[0].ToString().Replace("'", "''") + " : Last updated on " + item.Value.Split('~')[2].ToString().Replace("'", "''") + " : (" + item.Value.Split('~')[3].ToString().Replace("'", "''") + " seconds back)");
                                }
                            }
                            catch (Exception ex)
                            {
                                subject = "ERR in SCANS " + ex.Message;
                                string s = sm.Mail(to, subject, ex.Message);
                                Console.WriteLine("ERROR : " + ex.Message + " while sending mail to " + to);
                            }

                            Console.WriteLine();

                        }





                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR : " + ex.Message);
            }

        }



        private static void SP_SCAN_GET_TICKVOLUME_SPIKE(string strSectorId, string strEXCHANGE_SYMBOL, string strABBRs)
        {
            Logger.writeInLogFile("getStockUpdate() " + " DB " + ConnStrDBDisplayName);
            string sql = "";

            sql = "exec SP_SCAN_GET_TICKVOLUME_SPIKE " + strSectorId + "";


            bool isFound = false;
            try
            {
                // DateTime dtStockUpdateTime, dtTickTime;
                DataTable dtStkId = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, sql).Tables[0];
                Dictionary<string, string> stkList = new Dictionary<string, string>();
                if (dtStkId.Rows.Count > 1)
                {
                    //Console.WriteLine("DB : " + ConnStrDBDisplayName + " - Status : Healthy" + DateTime.Now.ToString());
                    //Logger.writeInLogFile("DB : " + ConnStrDBDisplayName + " - Status : Healthy");

                    for (int i = 1; i < 2; i++)
                    {
                        try
                        {

                            string strSql = "select * from scans where sp_name ='" + sql.Replace("'", "") + "' and stock_id ='" + dtStkId.Rows[i]["stock_id"].ToString() + "'";


                            DataTable dtscans = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, strSql).Tables[0];
                            if (dtStkId.Rows[i]["grown_perc"].ToString() != null && dtStkId.Rows[i]["volume_traded"].ToString() != null && dtStkId.Rows[i]["grown_perc"].ToString() != "" && dtStkId.Rows[i]["volume_traded"].ToString() != "")
                            {
                                if (dtscans.Rows.Count == 0 && Convert.ToDecimal(dtStkId.Rows[i]["grown_perc"].ToString()) > 5 && Convert.ToDecimal(dtStkId.Rows[i]["volume_traded"].ToString()) >20000)
                                {
                                    strSql = "insert into scans (SP_NAME,STOCK_ID) values ('" + sql.Replace("'", "") + "','" + dtStkId.Rows[i]["stock_id"].ToString() + "')";
                                    SqlHelper.ExecuteNonQuery(ConnStr, CommandType.Text, strSql);
                                    //   stkList.Add(dtStkId.Rows[i]["abbrs"].ToString() + "::" + dtStkId.Rows[i]["exchange_symbol"].ToString(), dtStkId.Rows[i]["abbrs"].ToString() + "~" + dtStkId.Rows[i]["exchange_symbol"].ToString() + "~" + dtStkId.Rows[i]["acc_volume"].ToString() + "~" + dtStkId.Rows[i]["avg_volume"].ToString() + "~" + dtStkId.Rows[i]["volume change (Times)"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i]["perc_change"].ToString() + "~" + dtStkId.Rows[i]["UPDATE_DATE_TIME"].ToString());
                                    stkList.Add(dtStkId.Rows[i]["stock_id"].ToString() + "::" + dtStkId.Rows[i]["insert_date_time"].ToString(), strABBRs + "~" + strEXCHANGE_SYMBOL + "~" + dtStkId.Rows[i]["grown_perc"].ToString() + "~" + dtStkId.Rows[i]["volume_traded"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i]["INSERT_DATE_TIME"].ToString());
                                    Console.WriteLine(dtStkId.Rows[i]["stock_id"].ToString() + " " + dtStkId.Rows[i]["grown_perc"].ToString());
                                    isFound = true;

                                    break;
                                }
                                else
                                {

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //string msg = dtStkId.Rows[i]["name"].ToString() + "~" + dtStkId.Rows[i]["code"].ToString();
                        }
                    }
                    TimeSpan startTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Open"].ToString());
                    TimeSpan endTimeTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Close"].ToString());
                    TimeSpan CurrTime = TimeSpan.Parse(DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":00");


                  
                        if (stkList.Count > 0)
                        {
                            string subject = "";
                            subject = "VOLUME_SPIKE - NIFTY 500 ";



                            string to = System.Configuration.ConfigurationManager.AppSettings["toId"].ToString();
                            SendMail sm = new SendMail();



                            string mailBody = "";
                            mailBody = mailBody + " NIFTY 500 VOLUME_SPIKE <br/><br/>";

                            mailBody = mailBody + "<table style=\"width:100%;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<tr style=\"align:center;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<th>Stock Name</th>";
                            mailBody = mailBody + "<th>Symbol</th>";
                            mailBody = mailBody + "<th>Rise in Volume (%)</th>";
                            mailBody = mailBody + "<th>Traded Volume</th>";

                            mailBody = mailBody + "<th>Last Price</th>";

                            mailBody = mailBody + "<th>Time</th>";
                            mailBody = mailBody + "</tr>";

                            foreach (var item in stkList)
                            {
                                mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                // mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";

                                //    mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + " %</td>";
                                //    Console.WriteLine("LOSER : " + item.Value.Split('~')[0].ToString().Replace("'", "''") + ". " + DateTime.Now.ToString());


                                //mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";

                                mailBody = mailBody + "</tr>";

                            }

                            mailBody = mailBody + "</table><br/><br/>";
                            mailBody = mailBody + " Thanks & Regards" + "<br/>";
                            mailBody = mailBody + " Tech Team, Mumbai" + "<br/>";
                            try
                            {
                                try
                                {
                                    string s = sm.Mail(to, subject, mailBody);
                                }
                                catch (Exception ex)
                                {

                                    // throw;
                                }



                                foreach (var item in stkList)
                                {
                                    mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                    mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";
                                    //mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + "</td>";
                                    //mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";


                                    mailBody = mailBody + "</tr>";

                                    //Console.WriteLine(item.Value.Split('~')[0].ToString().Replace("'", "''") + " : Last updated on " + item.Value.Split('~')[2].ToString().Replace("'", "''") + " : (" + item.Value.Split('~')[3].ToString().Replace("'", "''") + " seconds back)");
                                }
                            }
                            catch (Exception ex)
                            {
                                subject = "ERR in SCANS " + ex.Message;
                                string s = sm.Mail(to, subject, ex.Message);
                                Console.WriteLine("ERROR : " + ex.Message + " while sending mail to " + to);
                            }

                            Console.WriteLine();

                        }





                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR : " + ex.Message);
            }

        }



        private static void SP_SCANS_OFF_7_DAYS_LOW(int exchange_id)
        {
            Logger.writeInLogFile("getStockUpdate() " + " DB " + ConnStrDBDisplayName);
            string sql = "";

            sql = "exec SP_scan_get_off_7dayslow " + exchange_id;



            try
            {
                // DateTime dtStockUpdateTime, dtTickTime;
                DataTable dtStkId = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, sql).Tables[0];
                Dictionary<string, string> stkList = new Dictionary<string, string>();
                if (dtStkId.Rows.Count > 0)
                {
                    //Console.WriteLine("DB : " + ConnStrDBDisplayName + " - Status : Healthy" + DateTime.Now.ToString());
                    //Logger.writeInLogFile("DB : " + ConnStrDBDisplayName + " - Status : Healthy");

                    for (int i = 0; i < dtStkId.Rows.Count; i++)
                    {
                        try
                        {

                            string strSql = "select * from scans where sp_name ='" + sql.Replace("'", "") + "' and stock_id ='" + dtStkId.Rows[i]["stock_id"].ToString() + "'";


                            DataTable dtscans = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, strSql).Tables[0];

                            if (dtscans.Rows.Count == 0)
                            {
                                strSql = "insert into scans (SP_NAME,STOCK_ID) values ('" + sql.Replace("'", "") + "','" + dtStkId.Rows[i]["stock_id"].ToString() + "')";
                                SqlHelper.ExecuteNonQuery(ConnStr, CommandType.Text, strSql);
                                stkList.Add(dtStkId.Rows[i]["abbrs"].ToString() + "::" + dtStkId.Rows[i]["exchange_symbol"].ToString(), dtStkId.Rows[i]["abbrs"].ToString() + "~" + dtStkId.Rows[i]["exchange_symbol"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i]["low_7days"].ToString() + "~" + dtStkId.Rows[i]["off_7dayslow"].ToString() + "~" + dtStkId.Rows[i]["perc_change"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i]["UPDATE_DATE_TIME"].ToString());

                            }
                            else
                            {

                            }


                        }
                        catch (Exception ex)
                        {
                            //string msg = dtStkId.Rows[i]["name"].ToString() + "~" + dtStkId.Rows[i]["code"].ToString();
                        }
                    }
                    TimeSpan startTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Open"].ToString());
                    TimeSpan endTimeTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Close"].ToString());
                    TimeSpan CurrTime = TimeSpan.Parse(DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":00");
                
                        if (stkList.Count > 0)
                        {
                            string subject = "";
                            subject = "OFF 7DAYS LOW - NIFTY 500 ";



                            string to = System.Configuration.ConfigurationManager.AppSettings["toId"].ToString();
                            SendMail sm = new SendMail();



                            string mailBody = "";
                            mailBody = mailBody + " OFF 7DAYS LOW - NIFTY 500 <br/><br/>";

                            mailBody = mailBody + "<table style=\"width:100%;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<tr style=\"align:center;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<th>Stock Name</th>";
                            mailBody = mailBody + "<th>Symbol</th>";
                            mailBody = mailBody + "<th>last_price</th>";
                            mailBody = mailBody + "<th>low_7days</th>";
                            mailBody = mailBody + "<th>off_7dayslow</th>";

                            mailBody = mailBody + "<th>%</th>";
                            mailBody = mailBody + "<th>Volume (Lk)</th>";
                            mailBody = mailBody + "<th>Time</th>";
                            mailBody = mailBody + "</tr>";

                            foreach (var item in stkList)
                            {
                                mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                // mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "%</td>";

                                mailBody = mailBody + "<td>" + item.Value.Split('~')[6].ToString().Replace("'", "''") + " </td>";
                                Console.WriteLine(" OFF 7DAYS LOW - NIFTY 500 : " + item.Value.Split('~')[0].ToString().Replace("'", "''") + ". " + DateTime.Now.ToString());


                                mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";

                                mailBody = mailBody + "</tr>";

                            }

                            mailBody = mailBody + "</table><br/><br/>";
                            mailBody = mailBody + " Thanks & Regards" + "<br/>";
                            mailBody = mailBody + " Tech Team, Mumbai" + "<br/>";
                            try
                            {
                                try
                                {
                                    string s = sm.Mail(to, subject, mailBody);
                                }
                                catch (Exception ex)
                                {

                                    // throw;
                                }



                                foreach (var item in stkList)
                                {
                                    mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                    mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";


                                    mailBody = mailBody + "</tr>";

                                    //Console.WriteLine(item.Value.Split('~')[0].ToString().Replace("'", "''") + " : Last updated on " + item.Value.Split('~')[2].ToString().Replace("'", "''") + " : (" + item.Value.Split('~')[3].ToString().Replace("'", "''") + " seconds back)");
                                }
                            }
                            catch (Exception ex)
                            {
                                subject = "ERR in SCANS " + ex.Message;
                                string s = sm.Mail(to, subject, ex.Message);
                                Console.WriteLine("ERROR : " + ex.Message + " while sending mail to " + to);
                            }

                            Console.WriteLine();

                        }
                   



                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR : " + ex.Message);
            }

        }

        private static void SP_SCANS_VOL_SHOCKERS(int days, string variant)
        {
            Logger.writeInLogFile("getStockUpdate() " + " DB " + ConnStrDBDisplayName);
            string sql = "";

            sql = "exec SP_SCAN_VOLUME_SHOCKERS 1," + days + ",'1','" + variant + "'";



            try
            {
                // DateTime dtStockUpdateTime, dtTickTime;
                DataTable dtStkId = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, sql).Tables[0];
                Dictionary<string, string> stkList = new Dictionary<string, string>();
                if (dtStkId.Rows.Count > 0)
                {
                    //Console.WriteLine("DB : " + ConnStrDBDisplayName + " - Status : Healthy" + DateTime.Now.ToString());
                    //Logger.writeInLogFile("DB : " + ConnStrDBDisplayName + " - Status : Healthy");

                    for (int i = 0; i < dtStkId.Rows.Count; i++)
                    {
                        try
                        {

                            string strSql = "select * from scans where sp_name ='" + sql.Replace("'", "") + "' and stock_id ='" + dtStkId.Rows[i]["stock_id"].ToString() + "'";


                            DataTable dtscans = SqlHelper.ExecuteDataset(ConnStr, CommandType.Text, strSql).Tables[0];

                            if (dtscans.Rows.Count == 0)
                            {
                                strSql = "insert into scans (SP_NAME,STOCK_ID) values ('" + sql.Replace("'", "") + "','" + dtStkId.Rows[i]["stock_id"].ToString() + "')";
                                SqlHelper.ExecuteNonQuery(ConnStr, CommandType.Text, strSql);
                                stkList.Add(dtStkId.Rows[i]["abbrs"].ToString() + "::" + dtStkId.Rows[i]["exchange_symbol"].ToString(), dtStkId.Rows[i]["abbrs"].ToString() + "~" + dtStkId.Rows[i]["exchange_symbol"].ToString() + "~" + dtStkId.Rows[i]["acc_volume"].ToString() + "~" + dtStkId.Rows[i]["avg_volume"].ToString() + "~" + dtStkId.Rows[i]["volume change (Times)"].ToString() + "~" + dtStkId.Rows[i]["last_price"].ToString() + "~" + dtStkId.Rows[i]["perc_change"].ToString() + "~" + dtStkId.Rows[i]["UPDATE_DATE_TIME"].ToString());

                            }
                            else
                            {

                            }


                        }
                        catch (Exception ex)
                        {
                            //string msg = dtStkId.Rows[i]["name"].ToString() + "~" + dtStkId.Rows[i]["code"].ToString();
                        }
                    }
                    TimeSpan startTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Open"].ToString());
                    TimeSpan endTimeTime = TimeSpan.Parse(System.Configuration.ConfigurationManager.AppSettings["Close"].ToString());
                    TimeSpan CurrTime = TimeSpan.Parse(DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":00");
                    if (CurrTime >= startTime && CurrTime <= endTimeTime)
                    {
                        if (stkList.Count > 0)
                        {
                            string subject = "";
                            subject = "VOLUME SHOCKERS- NIFTY 500 " + variant + " with todays volume > 3X " + days + " day average volume";



                            string to = System.Configuration.ConfigurationManager.AppSettings["toId"].ToString();
                            SendMail sm = new SendMail();



                            string mailBody = "";
                            mailBody = mailBody + " NIFTY 500 " + variant + " with todays volume > 3X " + days + " day average volume" + "<br/><br/>";

                            mailBody = mailBody + "<table style=\"width:100%;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<tr style=\"align:center;border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                            mailBody = mailBody + "<th>Stock Name</th>";
                            mailBody = mailBody + "<th>Symbol</th>";
                            mailBody = mailBody + "<th>Todays Vol (Lk)</th>";
                            mailBody = mailBody + "<th>" + days + " Day Vol (Lk)</th>";
                            mailBody = mailBody + "<th>X Time Today Vol</th>";

                            mailBody = mailBody + "<th>Last Price</th>";
                            mailBody = mailBody + "<th>%</th>";
                            mailBody = mailBody + "<th>Time</th>";
                            mailBody = mailBody + "</tr>";

                            foreach (var item in stkList)
                            {
                                mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                // mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td align='center'>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                mailBody = mailBody + "<td  align='center'>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";
                                if (variant == "GAINER")
                                {
                                    mailBody = mailBody + "<td style=\"color:green\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + " %</td>";
                                    Console.WriteLine("GAINER: " + item.Value.Split('~')[0].ToString().Replace("'", "''") + ". " + DateTime.Now.ToString());
                                }
                                else
                                {
                                    mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + " %</td>";
                                    Console.WriteLine("LOSER : " + item.Value.Split('~')[0].ToString().Replace("'", "''") + ". " + DateTime.Now.ToString());
                                }

                                mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";

                                mailBody = mailBody + "</tr>";

                            }

                            mailBody = mailBody + "</table><br/><br/>";
                            mailBody = mailBody + " Thanks & Regards" + "<br/>";
                            mailBody = mailBody + " Tech Team, Mumbai" + "<br/>";
                            try
                            {
                                try
                                {
                                    string s = sm.Mail(to, subject, mailBody);
                                }
                                catch (Exception ex)
                                {

                                    // throw;
                                }



                                foreach (var item in stkList)
                                {
                                    mailBody = mailBody + "<tr style=\"border:1px solid black;border-collapse:collapse;cellspacing:5px;cellpadding:5px\">";
                                    mailBody = mailBody + "<td>" + item.Key.Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[0].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[1].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[2].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[3].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[4].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[5].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td style=\"color:red\">" + item.Value.Split('~')[6].ToString().Replace("'", "''") + "</td>";
                                    mailBody = mailBody + "<td>" + item.Value.Split('~')[7].ToString().Replace("'", "''") + "</td>";


                                    mailBody = mailBody + "</tr>";

                                    //Console.WriteLine(item.Value.Split('~')[0].ToString().Replace("'", "''") + " : Last updated on " + item.Value.Split('~')[2].ToString().Replace("'", "''") + " : (" + item.Value.Split('~')[3].ToString().Replace("'", "''") + " seconds back)");
                                }
                            }
                            catch (Exception ex)
                            {
                                subject = "ERR in SCANS " + ex.Message;
                                string s = sm.Mail(to, subject, ex.Message);
                                Console.WriteLine("ERROR : " + ex.Message + " while sending mail to " + to);
                            }

                            Console.WriteLine();

                        }
                    }



                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR : " + ex.Message);
            }

        }
        public static Boolean isHoliday()
        {
            bool holidayFlag = false;

            if (DateTime.Now.DayOfWeek.ToString().ToUpper() == "SATURDAY" || DateTime.Now.DayOfWeek.ToString().ToUpper() == "SUNDAY")
            {
                holidayFlag = true;
            }
            else
            {

                string sql = " exec SP_HOLIDAYCHECK";
                SqlHelper.ExecuteNonQuery(ConnStr, CommandType.Text, sql);
                try
                {
                    holidayFlag = false;
                }
                catch (Exception)
                {

                    holidayFlag = true;
                }

            }
            //  holidayFlag = false; // REMOVE THIS AFTER DIWALI
            return holidayFlag;
        }

        static void CheckCommodityStocks()
        {

            while (CheckDbFlag)
            {
                try
                {
                    Console.WriteLine("Checking Commodity Stocks");
                    // getComodityStockId(SqlConnStrMain);                    
                    //  Thread.Sleep(3000);// 
                }
                catch (Exception ex) { }
            }
        }



        private bool checkMailCondition(string type)
        {
            bool retValue = false;

            switch (type)
            {
                case "1":
                    if (Convert.ToInt32((DateTime.Now.Hour.ToString())) >= 9 && Convert.ToInt32((DateTime.Now.Hour.ToString())) <= 15)
                        if (Convert.ToInt32((DateTime.Now.Minute.ToString())) >= 15)
                            retValue = true;
                        else if (Convert.ToInt32((DateTime.Now.Hour.ToString())) >= 15 && Convert.ToInt32((DateTime.Now.Minute.ToString())) <= 30)
                            retValue = true;
                    break;
                case "2":
                    break;
                default:
                    break;
            }

            return retValue;


        }

        public static void KillProcess()
        {

            string startUpPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string myfileName = startUpPath + @"/MyProcess.txt";
            int iProcessID = 0;
            if (File.Exists(myfileName))
            {
                iProcessID = Convert.ToInt32(File.ReadAllText(myfileName).Trim());
            }
            try
            {
                if (iProcessID != 0)
                {
                    Process killprocess = Process.GetProcessById(iProcessID);
                    killprocess.Kill();
                }

            }
            catch (Exception)
            {

                //throw;
            }
            Process cProcess = Process.GetCurrentProcess();

            File.WriteAllText(myfileName, cProcess.Id.ToString());
        }
        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);

                Console.WriteLine("Ping Status of " + nameOrAddress + " :: " + reply.Status + " TimeDuration - " + reply.RoundtripTime.ToString());
                Logger.writeInLogFile("Ping Status of " + nameOrAddress + " :: " + reply.Status);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                string subject = "";
                string body = "";
                string from = "mukesh1.patel@zeemedia.esselgroup.com";
                string to = "mohit.bhan@zeemedia.esselgroup.com";
                string bcc = "";
                switch (nameOrAddress)
                {
                    case "10.3.5.14":
                        {
                            subject = "ALERT - MAIN DB : 10.3.5.14 Machine Down. Ignore Now. This is test mail";
                            body = "<b>Main Database ping status not responding. Main DB throwing RTO. Please check on an urgent basis</b>";
                            break;
                        }
                    case "10.3.5.15":
                        subject = "ALERT - BACKUP DB : 10.3.5.15 Machine Down. Ignore Now. This is test mail";
                        body = "<b>Backup Database ping status not responding. Backup DB throwing RTO.</b>";
                        break;
                    default:
                        break;
                }
                string mailBody = "";
                mailBody = "Dear All Concerned" + "<br/><br/>";
                mailBody = mailBody + " DB Alert Reported on  " + nameOrAddress + ". Please check and report immediately" + "<br/><br/>";
                mailBody = mailBody + body + "<br/><br/>";

                mailBody = mailBody + " Thanks & Regards" + "<br/>";
                mailBody = mailBody + " Dev Support, Mumbai" + "<br/>";

                string sqlInsert = "Insert into SendMail (FromId,ToId,Bcc,Subject,Body,Flag,insertdate) values (";
                sqlInsert += "'" + from + "','" + to + "','" + bcc + "','" + subject + "','" + mailBody + "','0',getdate())";
                SqlHelper.ExecuteNonQuery(ConnStr, CommandType.Text, sqlInsert);
                Console.WriteLine(body);
                Logger.writeInLogFile(body);


            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        public static void accessBSEData()
        {
            string sWebServiceUrl = "https://webservicename.com";

            // Create a Web service Request for the URL.           
            WebRequest objWebRequest = WebRequest.Create(sWebServiceUrl);

            //Create a proxy for the service request  
            objWebRequest.Proxy = new WebProxy();

            // set the credentials to authenticate request, if required by the server  
            objWebRequest.Credentials = new NetworkCredential("username", "password");
            objWebRequest.Proxy.Credentials = new NetworkCredential("username", "password");

            //Get the web service response.  
            HttpWebResponse objWebResponse = (HttpWebResponse)objWebRequest.GetResponse();

            //get the contents return by the server in a stream and open the stream using a                                                                   -            StreamReader for easy access.  
            StreamReader objStreamReader = new StreamReader(objWebResponse.GetResponseStream());

            // Read the contents.  
            string sResponse = objStreamReader.ReadToEnd();
            Console.WriteLine(sResponse);
            Console.ReadLine();

            // Cleanup the streams and the response.  
            objStreamReader.Close();
            objWebResponse.Close();
        }
    }
}
