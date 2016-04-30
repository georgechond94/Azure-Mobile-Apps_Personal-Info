using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure.Mobile.Server.Tables;

namespace uwpstoriesService.DataObjects
{
    public class UserInfo
    {
        public string Name { get; set; }
        public string ImageUri { get; set; }

    }
}