﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedDotServer
{
  class Program
  {
    static void Main()
    {
      var server = new RedDotTcpServer();
      server.Start();
    }
  }
}
