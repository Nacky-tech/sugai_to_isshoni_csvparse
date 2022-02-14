using System;

// ファイル読み書き用
using System.IO;

// 文字コード用
using System.Text;

// List, Dictionary クラス用
using System.Collections.Generic;

// Dictionary クラスのソート用
// リスト等のデータをあれこれしたい時は、Linqは結構便利
// c# をしっかり使いたいなら、Linqについて調べることをおすすめする
using System.Linq;

namespace station_list
{
    class Program
    {
        static void Main(string[] args)
        {
            CsvController.makeOutputFiles();
        }
    }


    //
    // 汎用的にどこでも使いそうなメソッドをまとめたクラス
    // 大体は、別途namespace を用意してそこで色々メソッドをまとめることが多い
    // namespace Common {
    //   ...
    //   class Utils{
    //      ...
    //   }
    //   ...
    // }
    // とか
    //
    class Utils
    {
        // 現在のUNITTIMEの取得
        public static string getCurrentUnixTime() {
            return (DateTime.Now.Ticks/10000000).ToString();
        }

        // .env ファイル使う用の簡易的な読み出しメソッド
        // 引数keyに該当する設定がない場合は、 null を返す
        public static string getEnv(string key) {
            try
            {
                using(StreamReader reader = new StreamReader(".env")){
                    string ret = null;
                    while(!reader.EndOfStream) {
                        string line = reader.ReadLine();
                        string[] keyValue = line.Split('=');
                        if ( key == keyValue[0] )
                        {
                            ret = keyValue[1];
                            break;
                        }
                    }
                    return ret;
                }
            }
            catch (System.Exception)
            {
                Console.WriteLine(".envファイルの読み出しに失敗しました。");
                throw;
            }
        }
    }


    class CsvController
    {
        // ファイル書き出し用パスを生成
        // identifier に指定がない場合は、現在のUNIXTIMEを使用する
        private static string getOutputPath(string filename, string identifier=null ){
            string pre = identifier != null ? identifier : Utils.getCurrentUnixTime();
            string OutputDirectory = Utils.getEnv("outDir");
                    if ( OutputDirectory == null) {
                        OutputDirectory = "out";
                    }
            return $"{OutputDirectory}/{identifier}/{filename}";
        }


        /**
        * [args]
        * string filepath : 読み込むデータファイルのパス
        * int startRowIndex : 読み込み時に先頭から何行目まで無視するか（0スタート）
        * int colIndex : 読み込み時に何個目のカンマ区切りの文字列について、取り扱うか（0スタート）
        * [return]
        * void
        * [output files]
        * 以下のファイルを出力する
        * ・文字の出現回数辞書
        * ・文字列と１６進数表記表
        */
        public static void makeOutputFiles(int startRowIndex=1, int colIndex=2) {
            try {
                // 入出力ファイル名
                // .envファイルで設定
                string InputFilePath = Utils.getEnv("input");
                string OutputDirectory = Utils.getEnv("outDir");
                    if ( OutputDirectory == null) {
                        OutputDirectory = "out";
                    }
                string OutputUsedUTF8CodeMapFileName = Utils.getEnv("codeMapFileName");
                string OutputNameCodeFileName = Utils.getEnv("nameListFileName");

                if (InputFilePath == null || OutputNameCodeFileName == null || OutputUsedUTF8CodeMapFileName == null) {
                    Console.WriteLine("入出力ファイルの設定を読み込めませんでした。[.env]ファイルを確認してください。");
                    return;
                }


                // 文字の出現回数辞書用変数
                // key: int, 文字（１文字）をint化したもの
                // value: int, 出現回数
                Dictionary<Int32, Int32> appearanceDictionary = new Dictionary<Int32, Int32>();

                // html用の出力CSVを文字列化したもの
                string[] csvStringForHtml = {"",""};


                // 現在の時間を取得。出力フォルダ名に使用
                string unitxtime = Utils.getCurrentUnixTime();
                // 出力用フォルダが存在しないならば作成
                if ( !Directory.Exists($"{OutputDirectory}/{unitxtime}") )
                {
                    Directory.CreateDirectory($"{OutputDirectory}/{unitxtime}");
                }


                // StreamWriter writer
                // 書き出し用のストリーム
                using ( StreamWriter writer = new StreamWriter(getOutputPath(OutputNameCodeFileName, unitxtime), false, Encoding.UTF8)) {
                    // タイトル行を書き出し
                    writer.WriteLine("文字列,１文字ずつの文字番号（１６進数）");

                    // StreamReader reader
                    // 読み出し用のストリーム
                    // ！！！１行ずつ処理！！！
                    using( StreamReader reader = new StreamReader(InputFilePath, Encoding.UTF8) ) {
                        int counter = 0;
                        while(!reader.EndOfStream) {
                            // 一行取り出し
                            string line = reader.ReadLine();

                            // 先頭から startRowIndex までの行は無視する
                            // reader.ReadLine()　呼び出し後に判定すること
                            counter++;
                            if ( counter <= startRowIndex) {
                                continue;
                            }


                            // カンマ区切りで配列化
                            string[] columns = line.Split(',');
                            // 文字列名取り出し
                            string stationName = columns[colIndex];

                            // 文字列カンマ区切り16進数表記用
                            // ファイル書き出し時に
                            // string.Join(',',codes);を使って楽したい
                            // 初期化：先頭に文字列をそのまま格納した状態
                            List<string> nameCodeList = new List<string>(){stationName};

                            // 文字列を一文字ずつ処理
                            foreach (char c in stationName)
                            {
                                // 文字の番号を取得
                                Int32 cint = Convert.ToInt32(c);
                                // 文字番号（16進数）を文字列として取得
                                string chexstr = string.Format("{0:X}", cint);
                                // リストに追加
                                nameCodeList.Add(chexstr);

                                //
                                // 出現辞書に登録・カウントアップ
                                //
                                // キーが未登録の場合はキーとバリューを追加
                                if ( !appearanceDictionary.ContainsKey(cint) )
                                {
                                    appearanceDictionary[cint] = 1;
                                }
                                // キーが既にある場合は、出現回数を追加
                                else
                                {
                                    appearanceDictionary[cint]++;
                                }
                            }

                            // 文字列と１６進数表記表ファイル書き出し
                            // カンマ区切り文字列として生成を１行を生成
                            string writeline = string.Join(',', nameCodeList);
                            csvStringForHtml[0] += writeline+"\n";
                            writer.WriteLine(writeline);
                        }
                    }// EO reader

                }// EO writer


                // 出現辞書ファイル書き出し
                // StreamWriter writer
                // 書き出し用のストリーム
                using ( StreamWriter writer = new StreamWriter(getOutputPath(OutputUsedUTF8CodeMapFileName, unitxtime), false, Encoding.UTF8)) {
                    // タイトル行を書き出し
                    writer.WriteLine("文字番号,文字番号（16進数）文字,出現回数");

                    // key(文字番号)でソート
                    IOrderedEnumerable<KeyValuePair<Int32, Int32>> sortedDictionary = appearanceDictionary.OrderBy(item=> item.Key);
                    foreach(var pair in sortedDictionary)
                    {
                        int cint = pair.Key;
                        int count = pair.Value;

                        char c = Convert.ToChar(cint);
                        string writeline = $"{cint},{cint:X},{c},{count}";
                        writer.WriteLine(writeline);
                        csvStringForHtml[1] += writeline+"\n";

                    }
                }
                Console.WriteLine("ファイルの読み出し・書き出しが完了しました。");



                //
                // html ファイル書き出し
                // htmlからcsv読み込もうとすると、http通信しないといけないので
                // scriptタグ内にデータを直書きしてしまう
                // データ直書き部分の識別子として、 {{ csvdata }} を利用
                //
                using( StreamReader reader = new StreamReader("html/namelist.html", Encoding.UTF8) ) {
                    string s = reader.ReadToEnd();
                    s = s.Replace("{{ csvdata }}", csvStringForHtml[0]);
                    using ( StreamWriter writer = new StreamWriter(getOutputPath("namelist.html", unitxtime), false, Encoding.UTF8)) {
                        writer.Write(s);
                    }
                }
                using( StreamReader reader = new StreamReader("html/codemap.html", Encoding.UTF8) ) {
                    string s = reader.ReadToEnd();
                    s = s.Replace("{{ csvdata }}", csvStringForHtml[1]);
                    using ( StreamWriter writer = new StreamWriter(getOutputPath("codemap.html", unitxtime), false, Encoding.UTF8)) {
                        writer.Write(s);
                    }
                }


            }
            catch(IOException e) {
                Console.WriteLine("ファイルの読み出し・書き出しに失敗しました。");
                
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
