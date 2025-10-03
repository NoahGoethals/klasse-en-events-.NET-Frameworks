using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace BoekwinkelApp
{
    //  ENUM
    public enum Verschijningsperiode
    {
        Dagelijks = 1,
        Wekelijks = 2,
        Maandelijks = 3
    }

    //  BASISKLASSE: BOEK 
    public class Boek
    {
        private string _isbn = "";
        private string _naam = "";
        private string _uitgever = "";
        private decimal _prijs;

        public string Isbn
        {
            get => _isbn;
            set => _isbn = value;
        }

        public string Naam
        {
            get => _naam;
            set => _naam = value;
        }

        public string Uitgever
        {
            get => _uitgever;
            set => _uitgever = value;
        }

        // clamp 5–50 via setter (nog steeds conform opdracht)
        public decimal Prijs
        {
            get => _prijs;
            set
            {
                if (value < 5m) _prijs = 5m;
                else if (value > 50m) _prijs = 50m;
                else _prijs = value;
            }
        }

        public Boek() { }

        public Boek(string isbn, string naam, string uitgever, decimal prijs)
        {
            Isbn = isbn;
            Naam = naam;
            Uitgever = uitgever;
            Prijs = prijs; // setter clamped
        }

        // Sterk gevalideerde invoer
        public virtual void Lees()
        {
            Isbn = Input.ReadNonEmpty("ISBN: ");
            Naam = Input.ReadNonEmpty("Naam: ");
            Uitgever = Input.ReadNonEmpty("Uitgever: ");
            Prijs = Input.ReadDecimalInRange("Prijs (5 - 50): ", 5m, 50m);
        }

        public override string ToString()
        {
            return $"[Boek] {Naam} (ISBN: {Isbn}), Uitgever: {Uitgever}, Prijs: {Prijs:C}";
        }
    }

    //  AFGELEIDE KLASSE: TIJDSCHRIFT 
    public class Tijdschrift : Boek
    {
        public Verschijningsperiode Periode { get; set; }

        public Tijdschrift() : base() { }

        public Tijdschrift(string isbn, string naam, string uitgever, decimal prijs, Verschijningsperiode periode)
            : base(isbn, naam, uitgever, prijs)
        {
            Periode = periode;
        }

        public override void Lees()
        {
            base.Lees();

            Console.WriteLine("Verschijningsperiode:");
            Console.WriteLine("  1) Dagelijks");
            Console.WriteLine("  2) Wekelijks");
            Console.WriteLine("  3) Maandelijks");
            int keuze = Input.ReadIntInRange("Kies (1-3): ", 1, 3);
            Periode = (Verschijningsperiode)keuze;
        }

        public override string ToString()
        {
            return $"[Tijdschrift] {Naam} (ISBN: {Isbn}), Uitgever: {Uitgever}, Prijs: {Prijs:C}, Periode: {Periode}";
        }
    }

    // HULP: UNIEKE ID GENERATOR 
    public static class BestellingIdGenerator
    {
        private static int _huidig = 0;
        public static int Volgende() => ++_huidig;
    }

    // EVENT 
    public delegate void BestellingGeplaatstHandler(object sender, string bericht);

    // GENERISCHE KLASSE: BESTELLING
    public class Bestelling<T> where T : Boek
    {
        private int _id;

        public int Id
        {
            get => _id;
            set => _id = BestellingIdGenerator.Volgende();
        }

        public T Item { get; set; }
        public DateTime Datum { get; set; } = DateTime.Now;
        public int Aantal { get; set; }
        public int? AbonnementMaanden { get; set; } // alleen voor tijdschriften

        public event BestellingGeplaatstHandler BestellingGeplaatst;

        public Bestelling(T item, int aantal, int? abonnementMaanden = null)
        {
            Id = 0; 
            Item = item;
            Aantal = aantal;
            AbonnementMaanden = abonnementMaanden;
        }

        public Tuple<string, int, decimal> Bestel()
        {
            decimal totalePrijs = BerekenTotaal();
            var tuple = Tuple.Create(Item.Isbn, Aantal, totalePrijs);

            string msg = AbonnementMaanden.HasValue && Item is Tijdschrift ts
                ? $"Bestelling #{Id}: abonnement op tijdschrift \"{Item.Naam}\" ({ts.Periode}), " +
                  $"{AbonnementMaanden} maanden, {Aantal} exemplaren per levering. Totaal: {totalePrijs:C}."
                : $"Bestelling #{Id}: boek \"{Item.Naam}\" (ISBN {Item.Isbn}), aantal {Aantal}. Totaal: {totalePrijs:C}.";

            BestellingGeplaatst?.Invoke(this, msg);
            return tuple;
        }

        private decimal BerekenTotaal()
        {
            decimal totaal = Item.Prijs * Aantal;

            if (AbonnementMaanden.HasValue && Item is Tijdschrift ts)
            {
                int leveringenPerMaand = ts.Periode switch
                {
                    Verschijningsperiode.Dagelijks => 30,
                    Verschijningsperiode.Wekelijks => 4,
                    Verschijningsperiode.Maandelijks => 1,
                    _ => 1
                };
                totaal *= (AbonnementMaanden.Value * leveringenPerMaand);
            }

            return totaal;
        }

        public override string ToString()
        {
            string basis = $"[Bestelling #{Id}] {Item.Naam} x {Aantal} op {Datum:g}";
            if (AbonnementMaanden.HasValue) basis += $" | Abonnement: {AbonnementMaanden} maanden";
            return basis;
        }
    }

    //  INPUT HELPER (harde validatie) 
    public static class Input
    {
        public static string ReadNonEmpty(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string? s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                Console.WriteLine("  -> Invoer mag niet leeg zijn.");
            }
        }

        public static int ReadInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int v)) return v;
                Console.WriteLine("  -> Geef een geheel getal in.");
            }
        }

        public static int ReadIntInRange(string prompt, int min, int max)
        {
            while (true)
            {
                int v = ReadInt(prompt);
                if (v < min || v > max)
                {
                    Console.WriteLine($"  -> Waarde moet tussen {min} en {max} liggen.");
                    continue;
                }
                return v;
            }
        }

        public static int ReadIntMin(string prompt, int min)
        {
            while (true)
            {
                int v = ReadInt(prompt);
                if (v < min)
                {
                    Console.WriteLine($"  -> Waarde moet ≥ {min} zijn.");
                    continue;
                }
                return v;
            }
        }

        public static decimal ReadDecimalInRange(string prompt, decimal min, decimal max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (!decimal.TryParse(Console.ReadLine(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal v))
                {
                    Console.WriteLine("  -> Geef een geldig decimaal getal in.");
                    continue;
                }
                if (v < min || v > max)
                {
                    Console.WriteLine($"  -> Waarde moet tussen {min} en {max} liggen.");
                    continue;
                }
                return v;
            }
        }
    }

    //  PROGRAM
    class Program
    {
        static void Main()
        {
            // NL/BE weergave met €
            Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("nl-BE");

            // Startcatalogus
            var boek1 = new Boek("978-90-01-00001", "C# Basis", "NoorderBoek", 39.95m);
            var boek2 = new Boek("978-90-01-00002", "Patterns in Practice", "ZuidUitgeverij", 55.00m); // clamp → 50
            var tijd1 = new Tijdschrift("977-12-34-00001", "Dev Weekly", "CodePress", 6.00m, Verschijningsperiode.Wekelijks);
            var tijd2 = new Tijdschrift("977-12-34-00002", "Tech Monthly", "BitHouse", 3.00m, Verschijningsperiode.Maandelijks); // clamp → 5

            var catalogus = new List<Boek> { boek1, boek2, tijd1, tijd2 };

            Console.WriteLine("=== Catalogus start ===");
            foreach (var i in catalogus) Console.WriteLine(i);
            Console.WriteLine();

            // Demo bestellingen en event
            BestellingGeplaatstHandler handler = (_, msg) =>
            {
                Console.WriteLine();
                Console.WriteLine(">> EVENT: " + msg);
                Console.WriteLine();
            };

            var b1 = new Bestelling<Boek>(boek1, 3);
            b1.BestellingGeplaatst += handler;
            var t1 = b1.Bestel();
            Console.WriteLine($"Tuple (Boek): (ISBN: {t1.Item1}, Aantal: {t1.Item2}, Totaal: {t1.Item3:C})");

            var b2 = new Bestelling<Tijdschrift>((Tijdschrift)tijd1, 2, 3);
            b2.BestellingGeplaatst += handler;
            var t2 = b2.Bestel();
            Console.WriteLine($"Tuple (Tijdschrift): (ISBN: {t2.Item1}, Aantal: {t2.Item2}, Totaal: {t2.Item3:C})");

            Console.WriteLine();
            Console.WriteLine("Druk op ENTER voor het menu...");
            Console.ReadLine();

            Menu(catalogus);
        }

        static void Menu(List<Boek> catalogus)
        {
            while (true)
            {
                Console.WriteLine("=== Hoofdmenu ===");
                Console.WriteLine("1) Toon catalogus");
                Console.WriteLine("2) Voeg boek toe");
                Console.WriteLine("3) Voeg tijdschrift toe");
                Console.WriteLine("4) Plaats bestelling (Boek)");
                Console.WriteLine("5) Plaats bestelling (Tijdschrift + abonnement)");
                Console.WriteLine("0) Afsluiten");

                int keuze = Input.ReadIntInRange("Keuze: ", 0, 5);
                Console.WriteLine();

                switch (keuze)
                {
                    case 1:
                        foreach (var i in catalogus) Console.WriteLine(i);
                        Console.WriteLine();
                        break;

                    case 2:
                        {
                            var b = new Boek();
                            b.Lees(); // bevat validatie
                            catalogus.Add(b);
                            Console.WriteLine("Boek toegevoegd.\n");
                            break;
                        }

                    case 3:
                        {
                            var t = new Tijdschrift();
                            t.Lees(); // bevat validatie incl. enum
                            catalogus.Add(t);
                            Console.WriteLine("Tijdschrift toegevoegd.\n");
                            break;
                        }

                    case 4:
                        {
                            Boek? b = KiesItem<Boek>(catalogus, onlyTijdschrift: false);
                            if (b == null) break;

                            int aantal = Input.ReadIntMin("Aantal (≥1): ", 1);

                            var bestelling = new Bestelling<Boek>(b, aantal);
                            bestelling.BestellingGeplaatst += (_, msg) => Console.WriteLine(">> EVENT: " + msg);
                            var tpl = bestelling.Bestel();
                            Console.WriteLine($"Tuple: (ISBN: {tpl.Item1}, Aantal: {tpl.Item2}, Totaal: {tpl.Item3:C})\n");
                            break;
                        }

                    case 5:
                        {
                            Tijdschrift? t = KiesItem<Tijdschrift>(catalogus, onlyTijdschrift: true);
                            if (t == null) break;

                            int aantal = Input.ReadIntMin("Aantal per levering (≥1): ", 1);
                            int maanden = Input.ReadIntMin("Abonnement (maanden, ≥1): ", 1);

                            var bestelling = new Bestelling<Tijdschrift>(t, aantal, maanden);
                            bestelling.BestellingGeplaatst += (_, msg) => Console.WriteLine(">> EVENT: " + msg);
                            var tpl = bestelling.Bestel();
                            Console.WriteLine($"Tuple: (ISBN: {tpl.Item1}, Aantal: {tpl.Item2}, Totaal: {tpl.Item3:C})\n");
                            break;
                        }

                    case 0:
                        return;
                }
            }
        }

        static T? KiesItem<T>(List<Boek> catalogus, bool onlyTijdschrift) where T : Boek
        {
            var lijst = new List<T>();
            int teller = 0;

            foreach (var item in catalogus)
            {
                if (onlyTijdschrift)
                {
                    if (item is Tijdschrift ts)
                    {
                        lijst.Add((T)(Boek)ts);
                        Console.WriteLine($"{++teller}. {ts}");
                    }
                }
                else
                {
                    if (item is T t)
                    {
                        lijst.Add(t);
                        Console.WriteLine($"{++teller}. {t}");
                    }
                }
            }

            if (lijst.Count == 0)
            {
                Console.WriteLine("Geen items gevonden.\n");
                return null;
            }

            int keuze = Input.ReadIntInRange("Kies nummer: ", 1, lijst.Count);
            return lijst[keuze - 1];
        }
    }
}
