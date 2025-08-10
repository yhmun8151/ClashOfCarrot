namespace DevelopersHub.ClashOfWhatever
{
    using UnityEngine;
    using System.Xml.Serialization;
    using System.IO;
    using System.Collections.Generic;
    using System;

    public class Util : MonoBehaviour
    {


        public static String gf_CommaValue(string number)
        {
            return string.Format("{0:N0}", long.Parse(number));
        }
    }
}