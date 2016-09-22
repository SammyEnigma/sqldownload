/********************************************************************/
/*  Language: C#                                       HCG:   1.0   */
/*                                                                  */
/*  File: Program.cs                                                */
/*  Description:                                                    */
/*                                                                  */
/*  Created By:  alexhart        Created At: 10/7/2010 11:22:00 AM  */
/*  Modified By: alexhart       Modified At: 10/7/2010 11:22:00 AM  */
/*                                                                  */
/********************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace SQLDownload
{
	/// <summary>
	/// Program Class Object
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Main Program procedure
		/// </summary>
		/// <param name="args">Command line arguments</param>
		static void Main(string[] args)
		{
            if (args.Length >= 2)
            {
                String connectionString = args[0];
                String path = args[1];
                SQLDownloader sd = new SQLDownloader(connectionString, path);
                sd.DownloadAll();
            }
            else
            {
                Console.Out.WriteLine("Usage: SQLDownload [ConnectionString] [PathToDownloadTo]");
            }

		}

	}
}
