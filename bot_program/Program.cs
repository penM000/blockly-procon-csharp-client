//U15長野プログラミングコンテスト実行委員会
//大会対戦用Bot
//
/* 敵を見つけたら逃げるようにする。偶然プットの可能性を低く
 * アイテムは20ターン毎でひとつ取得するようにする。
 * 同じ場所を行かないよう移動アルゴリズムをちょっとだけ賢く
*/
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using CHaser;

namespace u15NaganoBot
{
    public enum funcs
    {
        None, WalkRight, WalkLeft, WalkUp, WalkDown, LookRight, LookLeft, LookUp, LookDown,
        SearchRight, SearchLeft, SearchUp, SearchDown, PutRight, PutLeft, PutUp, PutDown,
    };

    class Program
    {
        const int nCOOL = 40000, nHOT = 50000;
        const int oCOOL = 2009, oHOT = 2010;

        const int Floor = 0, Enemy = 1, Block = 2, Item = 3;

        //接続用インスタンス
        private static Client Target = Client.Create();
        //private static Client Target = new Client(oHOT,"botA");

        //マップ記録用ハッシュテーブル
        private static Dictionary<Point, int> map = new Dictionary<Point, int>();

        private static int seed = Environment.TickCount;

        static void Main(string[] args)
        {
            //初期値
            int turn = 0, stepCount = 0, itemCount = 0;
            int hotoke_face = 0;

            bool safe = true;

            Point pos = new Point();//初期座標は0,0
            List<funcs> cmdList = new List<funcs>();//発行したコマンド記録
            List<Point> posList = new List<Point>();//過去の位置記録

            //移動経過記録
            posList.Add(pos);
            List<int[]> actionValue = new List<int[]>();//コマンド発行後の戻り値記録

            Random rnd;

            while (true)
            {
                stepCount++;
                turn++;

                funcs cmd = new funcs();
                int[] value = Target.GetReady();

                mapAdd(pos, value);

                rnd = new Random(seed++);

                //移動可能箇所の列挙
                var movableLocation = new List<funcs>() { funcs.WalkLeft, funcs.WalkUp, funcs.WalkRight, funcs.WalkDown, };

                //周囲に敵がいるかチェック
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == Enemy)
                    {
                        if (i == 1 || i == 3 || i == 5 || i == 7)
                        {
                            //仏の顔をインクリメント
                            hotoke_face++;

                            //仏の顔も三度まで
                            if (hotoke_face > 3) func(enemyCheck(value)); //即殺す、終了
                        }
                        //敵のいる方向は移動候補から除外する
                        movableLocation = enemyLocCalcRemoveList(i, movableLocation);

                        //危険フラグ
                        safe = false;
                        stepCount = -1;
                    }
                }

                //周囲情報から移動対象外を見つける
                for (int i = 1; i < 8; i += 2)
                {
                    if (value[i] == Block)
                    {
                        if (i == 1 && movableLocation.Contains(funcs.WalkUp)) movableLocation.Remove(funcs.WalkUp);
                        if (i == 3 && movableLocation.Contains(funcs.WalkLeft)) movableLocation.Remove(funcs.WalkLeft);
                        if (i == 5 && movableLocation.Contains(funcs.WalkRight)) movableLocation.Remove(funcs.WalkRight);
                        if (i == 7 && movableLocation.Contains(funcs.WalkDown)) movableLocation.Remove(funcs.WalkDown);
                    }
                }


                //アイテムを取っていいかどうかの計算、20ターンで１個取得、ダメなら移動候補から消去
                if (itemCount * 20 + 20 >= turn)　//まだアイテムを取れない
                {
                    List<funcs> removeCandidateList = new List<funcs>();
                    for (int i = 1; i < 8; i += 2)
                    {
                        if (value[i] == Item)
                        {
                            if (i == 1) removeCandidateList.Add(funcs.WalkUp);
                            else if (i == 3) removeCandidateList.Add(funcs.WalkLeft);
                            else if (i == 5) removeCandidateList.Add(funcs.WalkRight);
                            else removeCandidateList.Add(funcs.WalkDown);
                        }
                    }
                    //行ける方向が全てアイテムだった場合乱数で１箇所は残す
                    if (movableLocation.Count == removeCandidateList.Count)
                    {
                        removeCandidateList.Remove(removeCandidateList[rnd.Next(removeCandidateList.Count)]);
                    }
                    foreach (funcs remove in removeCandidateList)
                    {
                        movableLocation.Remove(remove);
                    }
                }

                //基本動作
                switch (turn)
                {
                    case 1:
                        //１ターン目はHotなら左か上
                        if (Target.Port == oHOT || Target.Port == nHOT)
                        {
                            cmd = movableLocation[rnd.Next(2)];
                        }
                        else
                        {
                            cmd = movableLocation[rnd.Next(2, 4)];
                        }
                        break;

                    default:
                        //前のコマンド方向をwalkに変換
                        funcs cmdListLast = cmdList.Last();
                        cmd = any2walk(cmdListLast);

                        //前回のアクションがLookで返り値に敵がいた場合その方向は除外する。
                        if (movableLocation.Count > 1 && (cmdListLast > funcs.WalkDown && cmdListLast < funcs.SearchRight) && safe)
                        {
                            int[] actionValueLast = actionValue.Last();
                            foreach (int val in actionValueLast)
                            {
                                if (val == Enemy)
                                {
                                    funcs lookToWalk = any2walk(cmdListLast);
                                    if (movableLocation.Contains(lookToWalk)) movableLocation.Remove(lookToWalk);
                                    safe = false;
                                    stepCount = -1;
                                }
                            }
                        }

                        //進行方向が壁か３歩後もしくは取ってはいけないアイテムなら方向転換とLook
                        if (!movableLocation.Contains(cmd) || stepCount % 4 == 0)
                        {
                            stepCount = 0;

                            if (movableLocation.Count() <= 1)
                            {
                                if (movableLocation.Count() == 1) cmd = walk2look(movableLocation.First());
                                else cmd = funcs.None; //行ける場所なし
                            }
                            else //移動可能方向が２方向以上
                            {
                                //来た方向が消せるなら消す
                                funcs reverceD = reverceDirection(cmdListLast);
                                if (movableLocation.Count > 1 && movableLocation.Contains(reverceD))
                                {
                                    movableLocation.Remove(reverceD);
                                }

                                //残った移動選択肢の中からひとつ削る
                                if (movableLocation.Count > 1)
                                {
                                    //計算と乱数を半々にする
                                    if (turn % 2 != 0) movableLocation.Remove(calcRemove(movableLocation, pos));
                                    else movableLocation.Remove(movableLocation[rnd.Next(movableLocation.Count)]);
                                }

                                //残ったものが複数あるばあい乱数で移動方向を決める
                                if (movableLocation.Count > 1)
                                {
                                    cmd = movableLocation[rnd.Next(movableLocation.Count)];
                                }
                                else cmd = movableLocation.First();

                                //敵がそばにいる場合はLookしないで即移動
                                if (safe) cmd = walk2look(cmd);
                                else safe = true;
                            }
                        }

                        break;
                }

                //移動先がアイテムならカウント
                if (move2GetItem(cmd, value)) itemCount++;

                //None命令等をチェック
                cmd = cmdNoneCheck(cmd);

                //実際のアクション
                value = func(cmd);

                //コマンド実行後の次の位置の計算
                pos = calcPos(posList.Last(), cmd, 1);

                //マップの更新
                mapAdd(pos, cmd, value);

                //cmdの追加
                cmdList.Add(cmd);
                posList.Add(pos);
                actionValue.Add(value);
            }
        }

        //移動先がアイテムかチェック
        private static bool move2GetItem(funcs cmd, int[] value)
        {
            bool result = false;
            if (cmd == funcs.WalkUp && value[1] == Item) result = true;
            if (cmd == funcs.WalkDown && value[7] == Item) result = true;
            if (cmd == funcs.WalkRight && value[5] == Item) result = true;
            if (cmd == funcs.WalkLeft && value[3] == Item) result = true;
            return result;
        }

        //敵と遭遇した場合行ったては行けない場所を除外する
        private static List<funcs> enemyLocCalcRemoveList(int direction, List<funcs> movableLocation)
        {
            //敵のいる方向は消去する。
            switch (direction)
            {
                case 0:
                    movableLocation.Remove(funcs.WalkLeft);
                    movableLocation.Remove(funcs.WalkUp);
                    break;
                case 1:
                    movableLocation.Remove(funcs.WalkUp);
                    break;
                case 2:
                    movableLocation.Remove(funcs.WalkUp);
                    movableLocation.Remove(funcs.WalkRight);
                    break;
                case 3:
                    movableLocation.Remove(funcs.WalkLeft);
                    break;
                case 4: //敵が乗っかってる？
                    movableLocation.Remove(funcs.WalkRight);
                    movableLocation.Remove(funcs.WalkUp);
                    movableLocation.Remove(funcs.WalkLeft);
                    movableLocation.Remove(funcs.WalkDown);
                    break;
                case 5:
                    movableLocation.Remove(funcs.WalkLeft);
                    break;
                case 6:
                    movableLocation.Remove(funcs.WalkLeft);
                    movableLocation.Remove(funcs.WalkDown);
                    break;
                case 7:
                    movableLocation.Remove(funcs.WalkDown);
                    break;
                case 8:
                    movableLocation.Remove(funcs.WalkDown);
                    movableLocation.Remove(funcs.WalkRight);
                    break;
                default:
                    break;
            }

            return movableLocation;
        }

        //マップを更新するメソッド
        private static void mapAdd(Point pos, funcs func, int[] value)
        {
            Point[] offsetPoint;

            switch (func)
            {
                case funcs.LookUp:
                    offsetPoint = new Point[9]{new Point(-1,-3), new Point(0,-3), new Point(1,-3),
                                               new Point(-1,-2), new Point(0,-2), new Point(1,-2),
                                               new Point(-1,-1), new Point(0,-1), new Point(1,-1),};
                    break;
                case funcs.LookDown:
                    offsetPoint = new Point[9]{new Point(-1,1), new Point(0,1), new Point(1,1),
                                               new Point(-1,2), new Point(0,2), new Point(1,2),
                                               new Point(-1,3), new Point(0,3), new Point(1,3),};
                    break;
                case funcs.LookRight:
                    offsetPoint = new Point[9]{new Point(1,-1), new Point(2,-1), new Point(3,-1),
                                               new Point(1,0),  new Point(2,0),  new Point(3,0),
                                               new Point(1,1),  new Point(2,1),  new Point(3,1),};
                    break;
                case funcs.LookLeft:
                    offsetPoint = new Point[9]{new Point(-3,-1), new Point(-2,-1), new Point(-1,-1),
                                               new Point(-3,0),  new Point(-2,0),  new Point(-1,0),
                                               new Point(-3,1),  new Point(-2,1),  new Point(-1,1),};
                    break;
                case funcs.SearchUp:
                    offsetPoint = new Point[9]{new Point(0,-1), new Point(0,-2), new Point(0,-3),
                                               new Point(0,-4), new Point(0,-5), new Point(0,-6),
                                               new Point(0,-7), new Point(0,-8), new Point(0,-9),};
                    break;
                case funcs.SearchDown:
                    offsetPoint = new Point[9]{new Point(0,1), new Point(0,2), new Point(0,3),
                                               new Point(0,4), new Point(0,5), new Point(0,6),
                                               new Point(0,7), new Point(0,8), new Point(0,9),};
                    break;
                case funcs.SearchRight:
                    offsetPoint = new Point[9]{new Point(1,0), new Point(2,0), new Point(3,0),
                                               new Point(4,0), new Point(5,0), new Point(6,0),
                                               new Point(7,0), new Point(8,0), new Point(9,0),};
                    break;
                case funcs.SearchLeft:
                    offsetPoint = new Point[9]{new Point(-1,0), new Point(-2,0), new Point(-3,0),
                                               new Point(-4,0), new Point(-5,0), new Point(-6,0),
                                               new Point(-7,0), new Point(-8,0), new Point(-9,0),};

                    break;
                case funcs.WalkUp:
                case funcs.WalkDown:
                case funcs.WalkRight:
                case funcs.WalkLeft:
                case funcs.PutUp:
                case funcs.PutDown:
                case funcs.PutRight:
                case funcs.PutLeft:
                default:
                    offsetPoint = new Point[9]{new Point(-1,-1), new Point(0,-1), new Point(1,-1),
                                               new Point(-1,0),  new Point(0,0),  new Point(1,0),
                                               new Point(-1,1),  new Point(0,1),  new Point(1,1),};
                    break;
            }
            //マップディクショナリの更新
            for (int i = 0; i < offsetPoint.Length; i++)
            {
                map[new Point(pos.X + offsetPoint[i].X, pos.Y + offsetPoint[i].Y)] = value[i];
            }

        }

        //GetReday用マップ更新メソッド
        private static void mapAdd(Point pos, int[] value)
        {
            Point[] offsetPoint = new Point[9]{new Point(-1,-1), new Point(0,-1), new Point(1,-1),
                                               new Point(-1,0),  new Point(0,0),  new Point(1,0),
                                               new Point(-1,1),  new Point(0,1),  new Point(1,1),};

            for (int i = 0; i < offsetPoint.Length; i++)
            {
                map[new Point(pos.X + offsetPoint[i].X, pos.Y + offsetPoint[i].Y)] = value[i];
            }
        }

        //行かなくても良さそうな方向をひとつ計算するメソッド
        private static funcs calcRemove(List<funcs> movableLocation, Point pos)
        {
            funcs function = new funcs();
            int minScore = int.MaxValue;

            for (int i = 0; i < movableLocation.Count; i++)
            {
                int score = 0;
                const int distance = 2;   //2歩進んだ先の周囲を探索
                Point targetPos = new Point();
                switch (movableLocation[i])
                {
                    case funcs.WalkRight:
                        targetPos = new Point(pos.X + distance, pos.Y);
                        break;
                    case funcs.WalkLeft:
                        targetPos = new Point(pos.X - distance, pos.Y);
                        break;
                    case funcs.WalkUp:
                        targetPos = new Point(pos.X, pos.Y - distance);
                        break;
                    case funcs.WalkDown:
                        targetPos = new Point(pos.X, pos.Y + distance);
                        break;
                }
                //目的地がどうなってるか知ってるか？
                if (map.ContainsKey(targetPos))
                {
                    if (map[targetPos] == Block) score -= 2; //目的地がブロックなら行く候補として魅力薄
                }

                //目的地の周囲情報のオフセット位置
                Point[] arroundPoint = new Point[9]{new Point(-1,-1), new Point(0,-1), new Point(1,-1),
                                                    new Point(-1,0),  new Point(0,0),  new Point(1,0),
                                                    new Point(-1,1),  new Point(0,1),  new Point(1,1),};

                foreach (Point p in arroundPoint)
                {
                    Point scanPos = new Point(targetPos.X + p.X, targetPos.Y + p.Y);
                    if (map.ContainsKey(scanPos))
                    {
                        if (map[p] == Block) score -= 2; //ブロックなら評価下げる
                        else score -= 1;
                    }
                }
                if (minScore > score)
                {
                    minScore = score;
                    function = movableLocation[i];
                }
                //スコア一緒なら乱数でどっちを採用するか決める
                else if (minScore == score)
                {
                    Random rnd = new Random(seed++);
                    if (rnd.Next(2) != 0) function = movableLocation[i];
                }
            }
            //ダメそうなやつを返す
            return function;
        }

        //Walkコマンドによって次の自位置を計算するメソッド
        private static Point calcPos(Point pos, funcs cmd, int distance)
        {
            if (cmd == funcs.WalkUp) pos.Y -= distance;
            if (cmd == funcs.WalkDown) pos.Y += distance;
            if (cmd == funcs.WalkRight) pos.X += distance;
            if (cmd == funcs.WalkLeft) pos.X -= distance;
            return pos;
        }

        //来た方向を計算するメソッド
        private static funcs reverceDirection(funcs cmd)
        {
            funcs ret = new funcs();
            if (cmd == funcs.WalkUp) ret = funcs.WalkDown;
            if (cmd == funcs.WalkDown) ret = funcs.WalkUp;
            if (cmd == funcs.WalkRight) ret = funcs.WalkLeft;
            if (cmd == funcs.WalkLeft) ret = funcs.WalkRight;
            return ret;
        }

        private static funcs walk2look(funcs cmd)
        {
            return cmd + 4;
        }

        private static funcs any2walk(funcs cmd)
        {
            if (cmd > funcs.SearchDown) cmd -= 12; //put
            else if (cmd > funcs.LookDown) cmd -= 8;//Search
            else if (cmd > funcs.WalkDown) cmd -= 4;//Look

            return cmd;
        }

        //敵と上下左右で出会った時の殺す命令
        private static funcs enemyCheck(int[] value)
        {
            funcs action = funcs.None;
            if (value[1] == 1) action = funcs.PutUp;
            if (value[3] == 1) action = funcs.PutLeft;
            if (value[5] == 1) action = funcs.PutRight;
            if (value[7] == 1) action = funcs.PutDown;

            return action;
        }

        //None命令ならLookかsearchどれかをランダムに発行する。
        private static funcs cmdNoneCheck(funcs data)
        {
            if (data == funcs.None || data > funcs.PutDown)
            {
                Random rnd = new Random(seed++);
                data = (funcs)rnd.Next(5, 13);// 5=LookRight, 12=SearchDown

                Console.WriteLine("None命令が発行されましたので{0}を実行しました", data);
            }
            return data;
        }

        //アクション
        private static int[] func(funcs data)
        {
            int[] result = new int[9];
            switch (data)
            {
                case funcs.WalkRight:
                    result = Target.WalkRight();
                    break;
                case funcs.WalkLeft:
                    result = Target.WalkLeft();
                    break;
                case funcs.WalkUp:
                    result = Target.WalkUp();
                    break;
                case funcs.WalkDown:
                    result = Target.WalkDown();
                    break;
                case funcs.LookRight:
                    result = Target.LookRight();
                    break;
                case funcs.LookLeft:
                    result = Target.LookLeft();
                    break;
                case funcs.LookUp:
                    result = Target.LookUp();
                    break;
                case funcs.LookDown:
                    result = Target.LookDown();
                    break;
                case funcs.SearchRight:
                    result = Target.SearchRight();
                    break;
                case funcs.SearchLeft:
                    result = Target.SearchLeft();
                    break;
                case funcs.SearchUp:
                    result = Target.SearchUp();
                    break;
                case funcs.SearchDown:
                    result = Target.SearchDown();
                    break;
                case funcs.PutRight:
                    result = Target.PutRight();
                    break;
                case funcs.PutLeft:
                    result = Target.PutLeft();
                    break;
                case funcs.PutUp:
                    result = Target.PutUp();
                    break;
                case funcs.PutDown:
                    result = Target.PutDown();
                    break;
                default:
                    result = Target.SearchUp(); //あり得ないが
                    break;
            }
            return result;
        }
    }
}