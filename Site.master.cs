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

        dt.Columns.Add(new DataColumn("tulos", typeof(string)));

        dt.Columns.Add(new DataColumn("logo2", typeof(string)));
        dt.Columns.Add(new DataColumn("tiimi2", typeof(string)));


        DataRow dr = dt.NewRow();

        List<JObject> games = getAllGames();
        bool olikoRiveja = false;
        string apuDate = "";
        foreach (var item in games)
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
                olikoRiveja = true;
            }

         
            
        }
        if (!olikoRiveja)
        {
            Label info = new Label();
            info.Attributes.Add("id", "infopanel");
            info.Text = "Yhtään ottelua ei löytynyt annetulla aikavälillä.";
            form1.Controls.Add(info);
        }
        return dt;
    }

    private List<string> listaaTiedot(JObject item, DateTime alkuaika, DateTime loppuaika)
    {
        List<string> ls = new List<string>();

        JToken itemJ = item.GetValue("MatchDate");
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

            string logo1 = item.GetValue("HomeTeam").SelectToken("LogoUrl").ToString();
            ls.Add(logo1);

            string tiimi1 = item.GetValue("HomeTeam").SelectToken("Name").ToString();
            ls.Add(tiimi1);

            ls.Add(item.GetValue("HomeGoals") + " - " + item.GetValue("AwayGoals"));

            string logo2 = item.GetValue("AwayTeam").SelectToken("LogoUrl").ToString();
            ls.Add(logo2);

            string tiimi2 = item.GetValue("AwayTeam").SelectToken("Name").ToString();
            ls.Add(tiimi2);

            return ls;
        }
        return new List<string>();
    }

    private void empty()
    {
        GridView2.Controls.Clear();
    }

    private DataRow createRecords(DataTable dt, List<string> rivilista)
    {
        DataRow dr = dt.NewRow();
        for(int i = 0; i<rivilista.Count;i++) dr[i] = rivilista[i];
        return dr;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
      if( GridView2.Rows.Count == 0) 
      gamelist();
    }
}
