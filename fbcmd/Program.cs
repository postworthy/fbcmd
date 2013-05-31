using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fbcmd
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool restart = false;
            do
            {
                try
                {
                    (new fbcmd()).Run();
                }
                catch(Exception ex) 
                {
                    if (ex.Message == "Logout")
                        restart = true;
                    else throw;
                }
            } while (restart);
        }
    }
}
