using Diag=System.Diagnostics;
using Gen=System.Collections.Generic;
using System.Linq;

using mwg.Term;
namespace mwg.Term{

	class Program{
		static void Main(string[] args){
			test1();
		}

		static void test1(){
			Diag::Process bashProcess = new Diag::Process();

			bashProcess.StartInfo.FileName="C:\\usr\\cygwin\\bin\\bash.exe"; 
			bashProcess.StartInfo.Arguments="--login -i "; //?--login
			bashProcess.StartInfo.WorkingDirectory="C:\\usr\\cygwin";

			//bashProcess.StartInfo.EnvironmentVariables["CYGWIN"] = "tty";

			bashProcess.StartInfo.RedirectStandardError=true;
			bashProcess.StartInfo.RedirectStandardInput=true;
			bashProcess.StartInfo.RedirectStandardOutput=true;
			bashProcess.StartInfo.CreateNoWindow=true;
			bashProcess.StartInfo.UseShellExecute=false;
			bashProcess.StartInfo.ErrorDialog=false;

			bashProcess.Start();

			bashProcess.OutputDataReceived+=(Diag::DataReceivedEventHandler)test1HandleStdout;
			bashProcess.ErrorDataReceived+=(Diag::DataReceivedEventHandler)test1HandleStderr;
			//bashProcess.BeginErrorReadLine();
			//bashProcess.BeginOutputReadLine();
			bashProcess.BeginOutputReadString();
			bashProcess.BeginErrorReadString();

			System.ConsoleKeyInfo ki;
			while(!bashProcess.HasExited&&(ki=System.Console.ReadKey(true)).Key!=System.ConsoleKey.Escape){
			  bashProcess.StandardInput.Write(((char)ki.KeyChar).ToString());
			}

			//int ch;
			//while(!bashProcess.HasExited&&(ch=System.Console.Read())>=0){
			//  bashProcess.StandardInput.Write(((char)ch).ToString());
			//}
		}

		static void test1HandleStdout(object sender,Diag::DataReceivedEventArgs e){
			try{
				if(e.Data!=null)
					System.Console.Write(e.Data.Replace('\x1b','e').Replace('\a','a'));
				else
					System.Console.WriteLine("(null)");
			}catch(System.Exception x){
				System.Console.ForegroundColor=System.ConsoleColor.Red;
				System.Console.WriteLine(x.ToString());
				System.Console.ResetColor();
			}
		}
		static void test1HandleStderr(object sender,Diag::DataReceivedEventArgs e){
			try{
				if(e.Data!=null)
					System.Console.Write(e.Data.Replace('\x1b','e').Replace('\a','a'));
				else
					System.Console.WriteLine("E: (null)");
			}catch(System.Exception x){
				System.Console.ForegroundColor=System.ConsoleColor.Red;
				System.Console.WriteLine(x.ToString());
				System.Console.ResetColor();
			}
		}
	}
}
