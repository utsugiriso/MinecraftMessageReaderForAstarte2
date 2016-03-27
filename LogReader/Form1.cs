using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using FNF.Utility;

namespace LogReader
{
    public partial class Form1 : Form
    {
        private static int LineCount = 0;

        private string getFilePath()
        {
            return Path.Combine(this.fileSystemWatcher1.Path, this.fileSystemWatcher1.Filter);
        }

        public Form1()
        {
            InitializeComponent();
            this.fileSystemWatcher1.Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft/logs");
            LineCount = File.ReadLines(getFilePath()).Count();

            this.comboBox1.DataSource = Enum.GetValues(typeof(VoiceType));

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "CharacterName";
            dataGridView1.Columns[0].DataPropertyName = "CharacterName";
            dataGridView1.Columns[1].Name = "Type";
            dataGridView1.Columns[1].DataPropertyName = "DisplayType";

            List<ReaderData> readerDataList = new List<ReaderData>();
            if(Properties.Settings.Default.ReaderData != null)
            {
                foreach (string readerDataString in Properties.Settings.Default.ReaderData)
                {
                    string[] splited = readerDataString.Split(',');
                    string characterName = splited[0];
                    string type = splited[1];
                    readerDataList.Add(new ReaderData(characterName, int.Parse(type)));
                }
            }

            BindingSource bs = new BindingSource();
            bs.DataSource = readerDataList;
            dataGridView1.DataSource = bs;
        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            string chatQuery = "[Client thread/INFO]: [CHAT]";
            string messageStartQuery = "「";

            int lineCount = 0;
            List<String> lineList = new List<string>();

            using (FileStream fileStream = new FileStream(getFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fileStream, System.Text.Encoding.Default))
                {
                    while (reader.Peek() > -1)
                    {
                        lineCount++;
                        string line = reader.ReadLine();
                        if (LineCount < lineCount)
                        {
                            if (line.Contains(chatQuery))
                            {
                                lineList.Add(line);
                            }
                        }
                    }
                }
            }
            LineCount = lineCount;

            if(lineList.Count == 0)
            {
                return;
            }

            BouyomiChanClient bc = new BouyomiChanClient();

            foreach(string line in lineList)
            {
                string message = line.Substring(line.IndexOf(messageStartQuery) + messageStartQuery.Length);
                int substringStartIndex = line.IndexOf(chatQuery) + chatQuery.Length;
                int subStringCount = line.IndexOf(messageStartQuery) - substringStartIndex;
                string characterName = line.Substring(substringStartIndex, subStringCount).Trim();
                VoiceType voicetype = VoiceType.Default;
                foreach (ReaderData readerData in (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource)
                {
                    if (readerData.CharacterName.Equals(characterName))
                    {
                        voicetype = (VoiceType)Enum.ToObject(typeof(VoiceType), readerData.Type);
                        break;
                    }
                }
                bc.AddTalkTask(message, -1, -1, -1, voicetype);
            }

            bc.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool exists = false;
            foreach(ReaderData readerData in (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource)
            {
                if(readerData.CharacterName.Equals(textBox1.Text))
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                ((BindingSource)dataGridView1.DataSource).Add(new ReaderData(textBox1.Text, (int)comboBox1.SelectedValue));
                saveSettings();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<ReaderData> readerDataList = (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource;
            List<string> deleteCharacterNameList = new List<string>();
            foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
            {
                if (!deleteCharacterNameList.Contains(readerDataList[cell.RowIndex].CharacterName))
                {
                    deleteCharacterNameList.Add(readerDataList[cell.RowIndex].CharacterName);
                }
            }

            foreach(string deleteCharacterName in deleteCharacterNameList)
            {
                foreach(ReaderData readerData in (BindingSource)dataGridView1.DataSource)
                {
                    if (deleteCharacterName.Equals(readerData.CharacterName))
                    {
                        ((BindingSource)dataGridView1.DataSource).Remove(readerData);
                        break;
                    }
                }
            }

            saveSettings();
        }

        private void saveSettings()
        {
            StringCollection stringCollection = new StringCollection();
            foreach (ReaderData readerData in (List<ReaderData>)((BindingSource)dataGridView1.DataSource).DataSource)
            {
                stringCollection.Add(readerData.CharacterName + "," + readerData.Type.ToString());
            }
            Properties.Settings.Default.ReaderData = stringCollection;
            Properties.Settings.Default.Save();
        }
    }
}
