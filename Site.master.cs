using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class SiteMaster : MasterPage
{
    private const string AntiXsrfTokenKey = "__AntiXsrfToken";
    private const string AntiXsrfUserNameKey = "__AntiXsrfUserName";
    private string _antiXsrfTokenValue;


    private List<JObject> getAllGames()
    {
        List<JObject> allGames = new List<JObject>();
        string aa = Directory.GetCurrentDirectory();
        using (StreamReader file = File.OpenText(Server.MapPath("~/matches.json")))
        using (JsonTextReader reader = new JsonTextReader(file))
        {
            JObject o2 = new JObject();
            foreach (var item in JToken.ReadFrom(reader))
            {
                allGames.Add((JObject)item);
            }
        }
        return allGames;
    }


    protected void Button1_Click(object sender, EventArgs e)
    {
        gamelist();
    }

    protected void gamelist()
    {
        empty();
        DateTime alkuaika = new DateTime(1, 1, 1);
        DateTime loppuaika = DateTime.Now;
        String[] aika = TextBox1.Text.Split('-', '/'); // vuosi-päivä-kuukausi
        String[] aika2 = TextBox2.Text.Split('-', '/');
        if (aika.Length == 3) alkuaika = new DateTime(int.Parse(aika[0]), int.Parse(aika[1]), int.Parse(aika[2]), 0, 0,0);
        if (aika2.Length == 3) loppuaika = new DateTime(int.Parse(aika2[0]), int.Parse(aika2[1]), int.Parse(aika2[2]), 23, 59, 59);

        getGameAtTime(alkuaika, loppuaika);
    }

    private void getGameAtTime(DateTime alkuaika, DateTime loppuaika)
    {
        GridView2.DataSource = GetData(alkuaika, loppuaika);
        GridView2.DataBind();
        // <!--DataSource='<%# GetData() %>' --> vaihtehtoisesti dridin puolella 
    }

    private object GetData(DateTime alkuaika, DateTime loppuaika)
    {
      //  This method creates a DataTable with four rows.  Each row has the
      //   following schema:
      //         pvm            string      12.4.2015(alkuajan ja loppuajan välillä)
      //         aika           string      14.00
      //         logo           string(IMAGE)
      //         tiimin1Nimi    string FC venus
      //         logo           string(IMAGE)
      //         tiimin2Nimi    string FC venus
      //         tulos          string      1 - 1

        DataTable dt = new DataTable();

        dt.Columns.Add(new DataColumn("pvm", typeof(string)));
        dt.Columns.Add(new DataColumn("aika", typeof(string)));

        dt.Columns.Add(new DataColumn("logo1", typeof(string)));
        dt.Columns.Add(new DataColumn("tiimi1", typeof(string)));

        dt.Columns.Add(new DataColumn("logo2", typeof(string)));
        dt.Columns.Add(new DataColumn("tiimi2", typeof(string)));

        dt.Columns.Add(new DataColumn("tulos", typeof(string)));
        DataRow dr = dt.NewRow();

        List<JObject> games = getAllGames();

        string apuDate = "";
        foreach (var item in games)  // jokainen peli
        {
            List<string> rivilista = listaaTiedot(item, alkuaika, loppuaika); // merkkijonolistana kaikki yhden rivin merkkijonot
            if (rivilista.Count != 0)
            {
                if (apuDate != rivilista[0])
                {
                    dr = createRecords(dt, new List<string>() { rivilista[0], "", "", "", "", "", "" });
                    dt.Rows.Add(dr);
                }
                apuDate = rivilista[0];
                rivilista[0] = "";
                dr = createRecords(dt, rivilista);
                dt.Rows.Add(dr);
            }
            else
            {
                Label info = new Label();
                info.Attributes.Add("id", "infopanel");
                info.Text += "Yhtään ottelua ei löytynyt annetulla aikavälillä.";
                form1.Controls.Add(info);
            }
        }
        return dt;
    }

    private List<string> listaaTiedot(JObject item, DateTime alkuaika, DateTime loppuaika)
    {
        List<string> ls = new List<string>();

        JToken itemJ = Hae("MatchDate", item);
        // 4.12.2015 12:12:12      
        // 4.12.2015 1.30.00 AM   
        bool onko12tuntia = itemJ.ToString().Contains("M");

        string[] t = itemJ.ToString().Split(' ');
        // jakaa pvm ja aika
        string date = t[0];
        string[] datesplit = date.Split('.', '/', '-');
        //jakaa kk/pp/vvvv

        string[] kellonaika = t[1].Split(':', ' ','.');
        // jakaa hh.mm.ss tai HH.mm.ss
        
         
        string time = t[1].Substring(0, 5); // 11.12 tai 11.00 tai 1.30:
        if (time.Contains(":")) time = time.Substring(0, 4);


        // 4.12.2015 12:12:12      
        //  4/12/2015 2:00:00 
        DateTime aika;
        try {
            aika = new DateTime(int.Parse(datesplit[2]), int.Parse(datesplit[0]), int.Parse(datesplit[1]), int.Parse(kellonaika[0]), int.Parse(kellonaika[1]), int.Parse(kellonaika[2]));
        }
        catch (System.ArgumentOutOfRangeException e) {
            aika = new DateTime(int.Parse(datesplit[2]), int.Parse(datesplit[1]), int.Parse(datesplit[0]), int.Parse(kellonaika[0]), int.Parse(kellonaika[1]), int.Parse(kellonaika[2]));
        }

        //hh1.InnerText += "datetimemuoto: " + aika.ToString() + "<<";
        //hh1.InnerText += "alkuaika:" + alkuaika.ToString() + "<<";
        //hh1.InnerText += "loppuaika" + loppuaika.ToString() + "<<";
        //hh1.InnerText += "aika" + aika.ToString() + "<<";
        //hh1.InnerText += "alkuvertaus:" + alkuaika.CompareTo(aika).ToString() + "<<";
        //hh1.InnerText += "loppuvertaus:" + loppuaika.CompareTo(aika).ToString() + "<<";
        //hh1.InnerText += "yritys" + aika.ToString("HH:mm") + "<<";

        if (alkuaika.CompareTo(aika) <= 0 && loppuaika.CompareTo(aika) >= 0)
         {
            
            string[] datepp = date.Split(',', '-', '/', '.');
             string dateN = datepp[1] + "/" + datepp[0] + "/" + datepp[2];
             ls.Add(dateN);  // pvm -> alunperin: kk/pp/vvvv -> muutettu -> pp/kk/vvvv

            if (t.Length > 2)
            {
                if (t[2] == "PM")
                {
                    int lasku = 0;
                    int.TryParse(kellonaika[0], out lasku);
                    lasku += 12;
                    kellonaika[0] = lasku.ToString();
                }
            }

            ls.Add(kellonaika[0] +":"+kellonaika[1]);  // aika ->  hh:mm

             JToken t1p = item.GetValue("HomeTeam");
             JToken logoJ = t1p.SelectToken("LogoUrl");
             string logo1 = logoJ.ToString();
             ls.Add(logo1);

             JToken a2 = item.GetValue("HomeTeam");
             JToken team1 = a2.SelectToken("Name");
             string tiimi1 = team1.ToString();
             ls.Add(tiimi1);

             JToken a4 = item.GetValue("HomeGoals");
             JToken a5 = item.GetValue("AwayGoals");
             ls.Add(a4 + " - " + a5);

             JToken t1p2 = item.GetValue("AwayTeam");
             JToken logoJ2 = t1p2.SelectToken("LogoUrl");
             string logo2 = logoJ2.ToString();
             ls.Add(logo2);

             JToken a3 = item.GetValue("AwayTeam");
             JToken team2 = a3.SelectToken("Name");
             string tiimi2 = team2.ToString();
             ls.Add(tiimi2);

             return ls;
        }
        return new List<string>();
    }

    private void empty()
    {
        GridView2.Controls.Clear();
    }

    private JToken Hae(string v, JObject item)
    {
        return item.GetValue(v);
    }

    private DataRow createRecords(DataTable dt, List<string> rivilista)
    {
        DataRow dr = dt.NewRow();
        dr[0] = rivilista[0];
        dr[1] = rivilista[1];
        dr[2] = rivilista[2];
        dr[3] = rivilista[3];
        dr["tulos"] = rivilista[4]; // TODO: miksi näin? estää silmukan. 
        dr[4] = rivilista[5];
        dr[5] = rivilista[6];
        return dr;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        gamelist();
    }
}