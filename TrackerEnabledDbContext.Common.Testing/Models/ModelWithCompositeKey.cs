﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerEnabledDbContext.Common.Testing.Models
{
    [TrackChanges]
    public class ModelWithCompositeKey
    {
        [Key,Column(Order =1)]
        public string Key1 { get; set; }

        [Key, Column(Order = 2)]
        public string Key2 { get; set; }
    }
}
