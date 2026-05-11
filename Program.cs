using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AATreePerformanceTest
{
    public class AANode
    {
        public int Key;
        public int Level;
        public AANode Left;
        public AANode Right;

        public AANode(int key, int level = 1)
        {
            Key = key;
            Level = level;
        }
    }

    public class AATree
    {
        private AANode _root;
        private int _compareCount;

        public AATree()
        {
            _root = null;
            _compareCount = 0;
        }

        public void ResetCompareCount() => _compareCount = 0;
        public int GetCompareCount() => _compareCount;

        private int GetLevel(AANode t) => t?.Level ?? 0;

        private AANode Skew(AANode t)
        {
            if (t == null) return null;
            if (t.Left != null && t.Left.Level == t.Level)
            {
                AANode L = t.Left;
                t.Left = L.Right;
                L.Right = t;
                return L;
            }
            return t;
        }

        private AANode Split(AANode t)
        {
            if (t == null) return null;
            if (t.Right != null && t.Right.Right != null &&
                t.Right.Right.Level == t.Level)
            {
                AANode R = t.Right;
                t.Right = R.Left;
                R.Left = t;
                R.Level++;
                return R;
            }
            return t;
        }

        public void Insert(int key)
        {
            _root = Insert(_root, key);
        }

        private AANode Insert(AANode t, int key)
        {
            if (t == null)
            {
                _compareCount++; // проверка на null
                return new AANode(key);
            }

            _compareCount++; // сравнение key с t.Key
            if (key < t.Key)
            {
                t.Left = Insert(t.Left, key);
            }
            else if (key > t.Key)
            {
                t.Right = Insert(t.Right, key);
            }
            else
            {
                return t; // дубликат не вставляется
            }

            t = Skew(t);
            t = Split(t);
            return t;
        }

        public bool Search(int key)
        {
            AANode t = _root;
            while (t != null)
            {
                _compareCount++;
                if (key == t.Key) return true;
                _compareCount++;
                if (key < t.Key) t = t.Left;
                else t = t.Right;
            }
            return false;
        }

        public void Delete(int key)
        {
            _root = Delete(_root, key);
        }

        private AANode Delete(AANode t, int key)
        {
            if (t == null)
            {
                _compareCount++;
                return null;
            }

            _compareCount++;
            if (key < t.Key)
            {
                t.Left = Delete(t.Left, key);
            }
            else if (key > t.Key)
            {
                t.Right = Delete(t.Right, key);
            }
            else
            {
                // узел найден
                if (t.Left == null && t.Right == null)
                {
                    return null;
                }
                else if (t.Left == null)
                {
                    AANode succ = t.Right;
                    while (succ.Left != null)
                    {
                        _compareCount++;
                        succ = succ.Left;
                    }
                    t.Key = succ.Key;
                    t.Right = Delete(t.Right, succ.Key);
                }
                else
                {
                    AANode pred = t.Left;
                    while (pred.Right != null)
                    {
                        _compareCount++;
                        pred = pred.Right;
                    }
                    t.Key = pred.Key;
                    t.Left = Delete(t.Left, pred.Key);
                }
            }

            int expectedLevel = 1 + Math.Min(GetLevel(t.Left), GetLevel(t.Right));
            if (expectedLevel < t.Level)
            {
                t.Level = expectedLevel;
                if (t.Right != null && t.Right.Level > expectedLevel)
                    t.Right.Level = expectedLevel;
            }

            t = Skew(t);
            if (t.Right != null) t.Right = Skew(t.Right);
            if (t.Right != null && t.Right.Right != null) t.Right.Right = Skew(t.Right.Right);
            t = Split(t);
            if (t.Right != null) t.Right = Split(t.Right);

            return t;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            const int totalInsert = 10000;
            const int totalSearch = 100;
            const int totalDelete = 1000;

            Random rand = new Random();

            // 2. Генерация массива случайных чисел
            int[] array = new int[totalInsert];
            for (int i = 0; i < totalInsert; i++)
            {
                array[i] = rand.Next(1, 100000);
            }

            // для выбора случайных индексов
            List<int> indices = Enumerable.Range(0, totalInsert).ToList();

            // ==================== 3. Тестирование вставки ====================
            AATree tree = new AATree();
            List<long> insertTimes = new List<long>();
            List<int> insertCompares = new List<int>();
            Stopwatch sw = new Stopwatch();

            for (int i = 0; i < totalInsert; i++)
            {
                tree.ResetCompareCount();
                sw.Restart();
                tree.Insert(array[i]);
                sw.Stop();

                insertTimes.Add(sw.ElapsedTicks);
                insertCompares.Add(tree.GetCompareCount());
            }

            double avgInsertTimeTicks = insertTimes.Average();
            double avgInsertTimeMs = avgInsertTimeTicks / TimeSpan.TicksPerMillisecond;
            double avgInsertCompares = insertCompares.Average();

            // ==================== 4. Тестирование поиска ====================
            var searchIndices = indices.OrderBy(x => rand.Next()).Take(totalSearch).ToList();
            List<long> searchTimes = new List<long>();
            List<int> searchCompares = new List<int>();

            foreach (int idx in searchIndices)
            {
                tree.ResetCompareCount();
                sw.Restart();
                tree.Search(array[idx]);
                sw.Stop();

                searchTimes.Add(sw.ElapsedTicks);
                searchCompares.Add(tree.GetCompareCount());
            }

            double avgSearchTimeTicks = searchTimes.Average();
            double avgSearchTimeMs = avgSearchTimeTicks / TimeSpan.TicksPerMillisecond;
            double avgSearchCompares = searchCompares.Average();

            // ==================== 5. Тестирование удаления ====================
            var deleteIndices = indices.OrderBy(x => rand.Next()).Take(totalDelete).ToList();
            List<long> deleteTimes = new List<long>();
            List<int> deleteCompares = new List<int>();

            foreach (int idx in deleteIndices)
            {
                tree.ResetCompareCount();
                sw.Restart();
                tree.Delete(array[idx]);
                sw.Stop();

                deleteTimes.Add(sw.ElapsedTicks);
                deleteCompares.Add(tree.GetCompareCount());
            }

            double avgDeleteTimeTicks = deleteTimes.Average();
            double avgDeleteTimeMs = avgDeleteTimeTicks / TimeSpan.TicksPerMillisecond;
            double avgDeleteCompares = deleteCompares.Average();

            // ==================== 6. Вывод результатов ====================
            Console.WriteLine("========== Результаты тестирования AA-дерева ==========");
            Console.WriteLine($"Вставка {totalInsert} раз:");
            Console.WriteLine($"  Среднее время: {avgInsertTimeMs:F4} мс ({avgInsertTimeTicks:F1} тиков)");
            Console.WriteLine($"  Среднее число сравнений: {avgInsertCompares:F2}");
            Console.WriteLine();
            Console.WriteLine($"Поиск {totalSearch} раз:");
            Console.WriteLine($"  Среднее время: {avgSearchTimeMs:F4} мс ({avgSearchTimeTicks:F1} тиков)");
            Console.WriteLine($"  Среднее число сравнений: {avgSearchCompares:F2}");
            Console.WriteLine();
            Console.WriteLine($"Удаление {totalDelete} раз:");
            Console.WriteLine($"  Среднее время: {avgDeleteTimeMs:F4} мс ({avgDeleteTimeTicks:F1} тиков)");
            Console.WriteLine($"  Среднее число сравнений: {avgDeleteCompares:F2}");
            Console.WriteLine("=======================================================");
        }
    }
}