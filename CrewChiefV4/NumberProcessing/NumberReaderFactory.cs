using CrewChiefV4.Audio;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.NumberProcessing
{
    class NumberReaderFactory
    {
        private static String NUMBER_READER_IMPL_SOURCE_NAME = "NumberReaderOverride.cs";
        private static NumberReaderFactory INSTANCE = new NumberReaderFactory();
        private NumberReader numberReader;

        private NumberReaderFactory()
        {
            //LoadImplmentationFromSource();
            // select the correct implementation for the language pack
            if ("it" == AudioPlayer.soundPackLanguage)
            {
                if (SoundPackVersionsHelper.currentSoundPackVersion >= 150)
                {
                    numberReader = new NumberReaderIt2();
                }
                else
                {
                    numberReader = new NumberReaderIt();
                }
            }
            else
            {
                numberReader = new NumberReaderEn();
            }
        }

        public static NumberReader GetNumberReader()
        {
            return INSTANCE.numberReader;
        }

        private Boolean LoadNumberReader(string code)
        {
            Microsoft.CSharp.CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters compilerparams = new CompilerParameters();
            compilerparams.GenerateExecutable = false;
            compilerparams.GenerateInMemory = true;
            compilerparams.ReferencedAssemblies.Add(typeof(NumberReader).Assembly.Location);
            CompilerResults results = provider.CompileAssemblyFromSource(compilerparams, code);
            if (results.Errors.HasErrors)
            {
                StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                foreach (CompilerError error in results.Errors)
                {
                    errors.AppendFormat("Line {0},{1}\t: {2}\n", error.Line, error.Column, error.ErrorText);
                }
                Console.WriteLine(errors.ToString());
                return false;
            }
            else
            {
                this.numberReader = (NumberReader) results.CompiledAssembly.CreateInstance("CrewChiefV4.NumberProcessing.NumberReaderOverride");
                return true;
            }        
        }

        private void LoadImplmentationFromSource()
        {
            StreamReader file = null;
            Boolean loadedOverride = false;
            try
            {
                file = new StreamReader(Configuration.getUserOverridesFileLocation(NUMBER_READER_IMPL_SOURCE_NAME));
                StringBuilder sb = new StringBuilder();
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }                
                LoadNumberReader(sb.ToString());
                loadedOverride = true;
            }
            catch (Exception) {}
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
            if (!loadedOverride)
            {
                StreamReader overrideFile = null;
                try
                {
                    overrideFile = new StreamReader(Configuration.getDefaultFileLocation(NUMBER_READER_IMPL_SOURCE_NAME));
                    StringBuilder sb = new StringBuilder();
                    String line;
                    while ((line = overrideFile.ReadLine()) != null)
                    {
                        sb.AppendLine(line);
                    }
                    LoadNumberReader(sb.ToString());
                }
                catch (Exception)
                {

                }
                finally
                {
                    if (overrideFile != null)
                    {
                        overrideFile.Close();
                    }
                }
            }
        }
    }
}
