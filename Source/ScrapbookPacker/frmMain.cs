using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;
using Wiry.Base32;

namespace ScrapbookPacker
{
    public partial class frmMain : Form
    {
        private string _sRootFolder;

        public frmMain()
        {
            InitializeComponent();

            String sAppPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().
                GetName().CodeBase)).LocalPath;
            _sRootFolder = Path.GetFullPath(Path.Combine(sAppPath, @"Data"));
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Text += $" [{_sRootFolder}]";
        }

        private void frmMain_DragDrop(object sender, DragEventArgs e)
        {
            foreach (string sFolder in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                try
                {
                    if (Directory.Exists(sFolder))
                    {
                        if (File.Exists(Path.Combine(sFolder, "index.html")))
                        {
                            textBox_Log.AppendText($"Processing {sFolder}" + Environment.NewLine);
                            FileInfo fi_Temp = new FileInfo(Path.Combine(_sRootFolder,
                                String.Format("Temp-{0}.zip", Guid.NewGuid().ToString())));
                            fi_Temp.Directory.Create();
                            ZipFile.CreateFromDirectory(sFolder, fi_Temp.FullName);
                            String sFilename = GetFilename(File.OpenRead(fi_Temp.FullName));
                            FileInfo fi_Final = new FileInfo(Path.Combine(_sRootFolder, sFilename));
                            if (!fi_Final.Exists)
                            {
                                fi_Final.Directory.Create();
                                File.Move(fi_Temp.FullName, fi_Final.FullName);
                                textBox_Log.AppendText($"  Save to {sFilename}" + Environment.NewLine);
                            }
                            else
                            {
                                textBox_Log.AppendText($"  Conflict - File Already Exists, {sFilename}" + Environment.NewLine);
                                fi_Temp.Delete();
                            }
                        }
                        else
                        {
                            textBox_Log.AppendText($"Skip {sFolder}, no index.html." + Environment.NewLine);
                        }
                    }
                    else
                    {
                        textBox_Log.AppendText($"Skip {sFolder}, it is not a folder." + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    textBox_Log.AppendText($"Exception: {ex.Message}, StackTrace: {ex.StackTrace}" + Environment.NewLine);
                }
            }
        }

        private void frmMain_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private String GetFilename(Stream stream)
        {
            using (stream)
            {
                SHA1Managed sha = new SHA1Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return GetFilename(Base32Encoding.Standard.GetString(checksum));
            }
        }

        private String GetFilename(String sHash)
        {
            try
            {
                byte[] checksum = Base32Encoding.Standard.ToBytes(sHash);
                List<String> subfolders = new List<String>();
                string subfolder = BitConverter.ToString(checksum).Replace("-", string.Empty);
                int index = 0;
                int total = subfolder.Length;
                while (index < total)
                {
                    subfolders.Add(subfolder.Substring(index, Math.Min(4, total - index)));
                    index += 4;
                }
                return String.Format(@"{0}\{1}.zip",
                    String.Join(@"\", subfolders.ToArray()), sHash);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
