﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Codeplex.Data;

namespace CHaser
{
    
    public class Client
    {
        //接続先
        public string server_address = "http://localhost:3000/";
        //ルーム名
        public string room_name = "game_server_13";


        //プレイヤー名
        public string name = "大会対戦用Bot";


        //ロジックタイム
        private double sleeptime = 0.1;

        //互換性用
        public int Port = 2010;




        //ここより下はいじらない(わかるなら良し)
        //クラス内変数宣言
        private Socket socket;                          //SocketIOのオブジェクト
        private bool getready=false;                    //getreadyフラグ
        private int[] response_data = null;             //returnする値
        private bool receive = false;                   //受信判定
        private bool exit_state = false;                //ゲーム終了判定

        

        public static Client Create()
        {
            return new Client();
        }


        private Client()
        {
            connect_server();
        }
        
        private void connect_server()
        {
            var message = new NewMessage()
            {
                room_id = room_name,
                name = name
            };
            // JObject型に変換
            var user = JObject.FromObject(message);


            Console.Write("サーバー接続開始\n");
            socket = IO.Socket(server_address);
      

            //Socketio イベント登録
            socket.On("joined_room", (obj) =>
            {
                Console.Write("joined_room\n#######\n");
                Console.WriteLine(obj);
                Console.Write("#######\n");
            });
            socket.On("error", (obj) =>
            {
                Console.Write("エラー\n");
                Console.WriteLine(obj);
                Console.Write("#######\n");
            });
            socket.On("game_result", (obj) =>
            {
                Console.Write("game_result\n#######\n");
                Console.WriteLine(obj);
                Console.Write("#######\n");
                exit_state = true;
                Console.Write("終了するにはなにかキーを押してください・・・\n");
                Console.Read();
                Environment.Exit(0);
            });

            socket.On("get_ready_rec", (obj) =>

            {
                

                //既に自分のターンならスキップ
                if (getready == true)
                {

                }
                //自分のターンでない場合
                else if (obj.ToString() == "{}" && exit_state == false)
                {
                    //Console.Write("notyourturn\n");
                    getready = false;
                }
                //自分のターンの場合
                else if(exit_state == false)
                {
                    var test = DynamicJson.Parse(obj.ToString());
                    response_data = test.rec_data;
                    //Console.Write("yourturn\n");
                    
                    getready = true;
                    receive = true;
                }
                //ゲームに勝敗がついている場合
                else
                {
                    
                }              
            });
            socket.On("look_rec", (obj) =>
            { 
                var test = DynamicJson.Parse(obj.ToString());
                response_data = test.rec_data;
                receive = true;
            });
            socket.On("search_rec", (obj) =>
            {
                var test = DynamicJson.Parse(obj.ToString());
                response_data = test.rec_data;
                receive = true;
            });
            socket.On("move_rec", (obj) =>
            {
                var test = DynamicJson.Parse(obj.ToString());
                response_data = test.rec_data;
                receive = true;
            });
            socket.On("put_rec", (obj) =>
            {
                var test = DynamicJson.Parse(obj.ToString());
                response_data = test.rec_data;
                receive = true;
            });
            socket.On(Socket.EVENT_CONNECT_ERROR, (obj) =>
            {
                Console.Write("サーバー接続に失敗しました\n");
                //Console.WriteLine(obj);
                
            });

            socket.On(Socket.EVENT_RECONNECTING , (obj) =>
            {
                Console.Write("サーバー接続に再接続しています\n");
                //Console.WriteLine(obj);

            });


            socket.On("connect", () =>
            {
                if (exit_state == false)
                {
                    socket.Emit("player_join", user);
                    Console.Write("サーバー接続完了\n");
                }
            });
        }


        private async Task sleepAsync(double second)
        {
            await Task.Delay((int)(second * 1000));
        }
        public void sleep(double second)
        {
            var task = sleepAsync(second);
            task.Wait();
        }
        public  int[] GetReady()
        {
            //you_turnが発火するまで待機
            while (getready==false){
                //非同期一時停止でSocketIOの受信処理続行
                socket.Emit("get_ready");
                sleep(sleeptime);
            }

            return response_data;
        }
        public int[] Ready() 
        {
            return GetReady();
        }


        public int[] WalkRight()
        {
            return Input("move_player", "right");
        }

        public int[] WalkLeft()
        {
            return Input("move_player", "left");
        }

        public int[] WalkUp()
        {
            return Input("move_player", "top");
        }

        public int[] WalkDown()
        {
            return Input("move_player", "bottom");
        }

        public int[] LookRight()
        {
            return Input("look", "right");
        }

        public int[] LookLeft()
        {
            return Input("look", "left");
        }

        public int[] LookUp()
        {
            return Input("look", "top");
        }

        public int[] LookDown()
        {
            return Input("look", "bottom");
        }

        public int[] SearchRight()
        {
            return Input("search", "right");
        }

        public int[] SearchLeft()
        {
            return Input("search", "left");
        }

        public int[] SearchUp()
        {
            return Input("search", "top");
        }

        public int[] SearchDown()
        {
            return Input("search", "bottom");
        }

        public int[] PutRight()
        {
            return Input("put_wall", "right");
        }

        public int[] PutLeft()
        {
            return Input("put_wall", "left");
        }

        public int[] PutUp()
        {
            return Input("put_wall", "top");
        }

        public int[] PutDown()
        {
            return Input("put_wall", "bottom");
        }

     

        public int[] Input(string mode,string direction)
        {
            //getreadyを行わず行動した場合
            if (getready == false)
            {
                //とりあえず前の結果を返す
                return response_data;
            }
            //getreadyが行われている場合
            else if (mode=="look" || mode=="search")
            {
                getready = false;
                receive = false;
                socket.Emit(mode, direction);
                while (receive == false)
                {
                    sleep(sleeptime);
                }
                return response_data;
            }
            else
            {
                getready = false;
                receive = false;
                socket.Emit(mode, direction);
                while (receive == false)
                {
                    sleep(sleeptime);
                }
                return response_data;
            }

        }

    }

    public class NewMessage
    {
        public String room_id { get; set; }
        public String name { get; set; }
    }



}
