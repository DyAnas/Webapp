﻿using System;

namespace GruppeInnlevering1.Models
{
    public class Avgang
    {
        public int AvgangId { get; set; }
        public TimeSpan Tid { get; set; }
        public virtual Tog Tog { get; set; }
        public virtual Stasjon Stasjon { get; set; }

    }
}



public class avgangs
{
    public int AvgangId { get; set; }
    public TimeSpan Tid { get; set; }

    public int TogId { get; set; }

    public int StasjonId { get; set; }
   
}