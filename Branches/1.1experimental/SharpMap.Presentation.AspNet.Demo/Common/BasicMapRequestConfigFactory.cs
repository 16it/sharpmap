﻿/*
 *  The attached / following is part of SharpMap.Presentation.AspNet
 *  SharpMap.Presentation.AspNet is free software © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
using System;
using System.Drawing;
using System.Web;

namespace SharpMap.Presentation.AspNet.Demo
{
    public class BasicMapRequestConfigFactory
        : IMapRequestConfigFactory<BasicMapRequestConfig>
    {
        #region IMapRequestConfigFactory<BasicMapRequestConfig> Members

        public BasicMapRequestConfig CreateConfig(HttpContext context)
        {

            BasicMapRequestConfig config = new BasicMapRequestConfig();
            config.Context = context;

            string soutputsize = context.Request.QueryString["outputsize"];

            bool useDefaultSize = true;
            if (!string.IsNullOrEmpty(soutputsize))
            {
                string[] parts = soutputsize.Split('x');
                try
                {
                    if (parts.Length == 2)
                    {
                        int width, height;
                        width = int.Parse(parts[0]);
                        height = int.Parse(parts[1]);
                        config.OutputSize = new Size(width, height);
                        useDefaultSize = false;
                    }
                }
                catch { }
            }
            if (useDefaultSize)
                config.OutputSize = new Size(400, 400);

            if (context.Request["BBOX"] != null)
                config.RealWorldBounds = SharpMap.Web.Wms.WmsServer.ParseBBOX(context.Request["BBOX"]);


            //ensure that the differences in the string diverges quickly by reversing it.
            char[] arr = context.Request.Url.PathAndQuery.ToLower().ToCharArray();
            Array.Reverse(arr);
            config.CacheKey = new string(arr);

            config.MimeType = context.Request["mimeType"] ?? "image/png";

            //config.CacheKey = context.Request.Url.ToString().ToLower();

            return config;

        }

        #endregion


        #region IMapRequestConfigFactory Members

        IMapRequestConfig IMapRequestConfigFactory.CreateConfig(HttpContext context)
        {
            return this.CreateConfig(context);
        }

        #endregion
    }
}
