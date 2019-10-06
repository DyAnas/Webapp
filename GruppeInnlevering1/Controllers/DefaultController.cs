﻿using GruppeInnlevering1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Windows;
using System.Windows.Forms;

namespace GruppeInnlevering1.Controllers
{
    public class DefaultController : Controller
    {
        TogContext db = new TogContext(); 


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
   


        public ActionResult Index()   
        {
            Samle ny = new Samle();   

            IEnumerable<Stasjon> alleStasjoner=  hentStasjoner();

            ny.fraListe= alleStasjoner;

            Session["alle"]= ny;     //lagre objekt i Session til å kunne bruke det i andre sider

            return View(ny);
        }




        // metode som begrenser valg i TilListe til kun de stasjonene som ligger i denne strekningen 
        // den skal retunere En Liste med Ajax-kall og json 

        public string hentListe(int stajonId)
        {
            IEnumerable<Stasjon> TilListe = hentTilListe(stajonId);


            List<Stasjon> valgStasjon = new List<Stasjon>();

            foreach (Stasjon i in TilListe)
            {
                valgStasjon.Add(new Stasjon { StasjonId = i.StasjonId, StasjonNavn = i.StasjonNavn });

            }

            Samle ny = (Samle)Session["alle"];
            ny.tilListe = valgStasjon;

            var jsonSeralizer = new JavaScriptSerializer();

            return jsonSeralizer.Serialize(ny.tilListe);

        }




        [HttpPost]
        public ActionResult Result(Samle s)
        {
            
            Samle ny = (Samle)Session["alle"];

            ny.antall1 = s.antall1;    //antall  StudentBilletter
            ny.antall2 = s.antall2;    //antall  VoksenBilletter
            ny.antall3 = s.antall3;    //antall  BarnBilletter
            ny.dato = s.dato;
            int result;
            int result1;
            IEnumerable<Avgang> turListe = null;
            try
            {
                 result = int.Parse(s.Fra);  //konvertere StajonId til Int 
                 result1 = int.Parse(s.Til); //DestnasjonId
            }

            //Hvis Destinasjon ikke er valgt blir man sendt til Index siden
            catch(Exception feil)
            {
                Response.Write("<script>alert('" + Server.HtmlEncode(feil.ToString()) + "')</script>");
                return RedirectToAction("Index");

                
            }
            IEnumerable<Avgang> ReturListe = null;

            //henter Tur og Retur Reiser avhenger av id til Stasjoner som ble valgt 
            if (result < result1)
            {
                                           
                turListe = db.Avganger.Where(b => b.Stasjon.StasjonId == result || b.Stasjon.StasjonId == result1);

                if (s.datoTilbake.GetHashCode() != 0)
                {
                    ny.datoTilbake = s.datoTilbake;

                    ReturListe = db.Avganger.Where(b => (b.Stasjon.StasjonId == result1) && b.Tog.TogId % 3 == 0 
                    || (b.Stasjon.StasjonId == result) && b.Tog.TogId % 3 == 0);


                }
            }

            //henter Tur og Retur Reiser avhenger av id til Stasjoner som ble valgt 

            else if (result > result1)
            {
                turListe = db.Avganger.Where(b => (b.Stasjon.StasjonId == result1) && b.Tog.TogId % 3 == 0 ||
                (b.Stasjon.StasjonId == result) && b.Tog.TogId % 3 == 0);


                if (s.datoTilbake.GetHashCode() != 0)
                {
                    ny.datoTilbake = s.datoTilbake;

                    ReturListe = db.Avganger.Where(b => b.Stasjon.StasjonId == result || b.Stasjon.StasjonId == result1).Take(4);

                }


            }
            //sette Tur og Retur Avganger i en Liste og sende dem til Viewen

            List<Avgang> TurogRetur = new List<Avgang>();

            foreach (Avgang avgang in turListe)
            {
                TurogRetur.Add(avgang);
            }
            if (s.datoTilbake.GetHashCode() != 0)
            {
                foreach (Avgang avgang in ReturListe)
                {
                    TurogRetur.Add(avgang);
                }
            }

            return View(TurogRetur);
        }



        public ActionResult Bekrefte(string FraStasjon, string TilStasjon, 
                                       TimeSpan Avgang, TimeSpan Ankomst,
                                       int StasjonfraId, int StasjonTilId)


        {

            Samle ny = (Samle)Session["alle"];     //henter objektet med Session for å bruke det i siden her 


            ny.Fra = FraStasjon;
            ny.Til = TilStasjon;
            ny.tidFra = Avgang;
            ny.tidTil = Ankomst;
            ny.stasjonIdTil = StasjonTilId;
            ny.stasjonIdFra = StasjonfraId;


            Session["alle"] = ny;

            return View(ny);
        }




        [HttpPost]
        public ActionResult Betaling(string Telefonnummer, string Email, string kortnummer, int Cvc)
        {

            Samle ny = (Samle)Session["alle"];


            //For å sette datoen til null i databasen har vi brukt koden under,
           
            //... brukte 9999 9 99 som en måte å nullstille Datoen , i vår databasen når det står 9999 9 99 det betyr at det ikke en retur reise
            //vi kunne bruke nullable til datoen, men vi synes at det  ikke  er logiskk å bruke det med datoen 


            if (ny.datoTilbake.GetHashCode() == 0)
            {

                ny.datoTilbake = new DateTime(9999, 9, 9);
            }



            var lengde = ny.stasjonIdTil - ny.stasjonIdFra;    //finner lengde mellom stasjoner til å regne prisen 


            //ligge Billtter for StudentType til databasen

            for (var i = 0; i < ny.antall1; i++)
            {
                var bnyBi = new Billet
                {
                    AvgangFra = ny.stasjonIdFra,
                    AvgangTil = ny.stasjonIdTil,
                    Type = "Student",
                    Pris = lengde * 10,
                    DatoTur = ny.dato,
                    DatoRetur = ny.datoTilbake,
                    Telefonnummer = Telefonnummer,
                    Email = Email,
                    Kortnummer = kortnummer,
                    Cvc = Cvc

                };
                try
                {
                    db.Billeter.Add(bnyBi);
                }
                catch (Exception)
                {
                    throw new Exception("kan ikke sette ny Billett i databasen");
                }

            }





            //ligge Billtter for VoksenType til databasen

            for (var i = 0; i < ny.antall2; i++)
            {
                var NyBillett = new Billet
                {
                    AvgangFra = ny.stasjonIdFra,
                    AvgangTil = ny.stasjonIdTil,
                    Type = "Voksen",
                    Pris = lengde * 20,
                    DatoTur = ny.dato,
                    DatoRetur = ny.datoTilbake,
                    Telefonnummer = Telefonnummer,
                    Email = Email,
                    Kortnummer = kortnummer,
                    Cvc = Cvc
                };


                try
                {
                    db.Billeter.Add(NyBillett);
                }
                catch(Exception feil)
                {
                    throw new Exception("kan ikke sette ny Billett i databasen");
                   
                }

            }




            //ligge Billtter for BarnType til databasen

            for (var i = 0; i < ny.antall3; i++)
            {
                var NyBillet = new Billet
                {
                    AvgangFra = ny.stasjonIdFra,
                    AvgangTil = ny.stasjonIdTil,
                    Type = "Barn",
                    Pris = lengde * 5,
                    DatoTur = ny.dato,
                    DatoRetur = ny.datoTilbake,
                    Telefonnummer = Telefonnummer,
                    Email = Email,
                    Kortnummer = kortnummer,
                    Cvc = Cvc
                };

                try
                {
                    db.Billeter.Add(NyBillet);
                }

                catch(Exception feil)
                {
                    throw new Exception("kan ikke sette ny Billett i databasen");
                    
                }

            }

            db.SaveChanges();

            return View();

        }


























        public IEnumerable<Stasjon> hentStasjoner()     //Henter Alle StasjonerListe til Select for Fra i Index Siden
        {
            IEnumerable<Stasjon> stasjoner = db.Stasjoner;

            return stasjoner;
        }




        public IEnumerable<Stasjon> hentTilListe(int id)
        {
            IEnumerable<Stasjon> tilListe = null;

            if (id > 0 && id < 9)
            {
                tilListe = db.Stasjoner.Where(p => p.StasjonId > 0 && p.StasjonId < 9);
            }
            else if (id > 8 && id < 17)
            {
                tilListe = db.Stasjoner.Where(p => p.StasjonId > 8 && p.StasjonId < 17);
            }

            else
            {
                tilListe = db.Stasjoner.Where(p => p.StasjonId > 16 && p.StasjonId < 25);
            }
            return tilListe;




        }


    }







}







