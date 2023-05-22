using PortableCleaner.Struct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableCleaner
{
    public class CleaningManager
    {
        private static void Swap(ref List<StructHole> list, int a, int b)
        {
            StructHole temp = list[a];
            list[a] = list[b];
            list[b] = temp;
        }

        private static int Partition(ref List<StructHole> list, int left, int right)
        {
            StructHole pivot;
            int low, high;

            low = left;
            high = right;
            pivot = list[left];

            do
            {
                do
                {
                    low++;
                }
                while (low < right && list[low].Y < pivot.Y);

                do
                {
                    high--; //high는 right 에서 시작
                } while (high >= left && list[high].Y > pivot.Y);

                // 만약 low와 high가 교차하지 않았으면 list[low]를 list[high] 교환
                if (low < high)
                {
                    Swap(ref list, low, high);
                }
            }
            while (low < high);

            Swap(ref list, left, high);

            return high;
        }

        private static void QuickSort(ref List<StructHole> list, int left, int right)
        {
            if (left < right)
            {
                int q = Partition(ref list, left, right);

                QuickSort(ref list, left, q - 1);
                QuickSort(ref list, q + 1, right);
            }
        }

        private static void Merge(ref List<StructHole> list, int left, int mid, int right)
        {
            int i, j, k, l;
            i = left;
            j = mid + 1;
            k = left;

            try
            {
                /* 분할 정렬된 list의 합병 */
                while (i <= mid && j <= right)
                {
                    if (list[i].Y <= list[j].Y && list[i].X <= list[j].X)
                        sorted[k++] = list[i++];
                    else
                        sorted[k++] = list[j++];
                }
            }
            catch
            {

            }

            try
            {
                // 남아 있는 값들을 일괄 복사
                if (i > mid)
                {
                    for (l = j; l <= right; l++)
                        sorted[k++] = list[l];
                }
                // 남아 있는 값들을 일괄 복사
                else
                {
                    for (l = i; l <= mid; l++)
                        sorted[k++] = list[l];
                }
            }
            catch
            {

            }

            try
            {
                // 배열 sorted[](임시 배열)의 리스트를 배열 list[]로 재복사
                for (l = left; l <= right; l++)
                {
                    list[l] = sorted[l];
                }
            }
            catch
            {

            }
        }

        private static void MergeSort(ref List<StructHole> list, int left, int right)
        {
            int mid;

            if (left < right)
            {
                mid = (left + right) / 2; // 중간 위치를 계산하여 리스트를 균등 분할 -분할(Divide)
                MergeSort(ref list, left, mid); // 앞쪽 부분 리스트 정렬 -정복(Conquer)
                MergeSort(ref list, mid + 1, right); // 뒤쪽 부분 리스트 정렬 -정복(Conquer)
                Merge(ref list, left, mid, right); // 정렬된 2개의 부분 배열을 합병하는 과정 -결합(Combine)
            }
        }

        private static StructHole[] sorted;

        public static List<StructHole> SortHole(List<StructHole> list, double range, double filterRangeMin, double filterRangeMax)
        {
            List<StructHole> result = new List<StructHole>();

            int column = 0;
            int row = 0;
            bool reverse = false;//InspectionInfo.IsHorizentalReverse;

            //InspectionInfo.DesPoints.Clear();

            list = list.OrderBy(point => point.Y).ToList();

            list = list.OrderBy(point => point.X).ToList();

            double xDistanceAvg = 0;
            int xDistanceCount = 0;

            for (int i = 0; i < list.Count - 1; i++)
            {
                double distance = Math.Sqrt(Math.Pow(list[i].X - list[i + 1].X, 2) + Math.Pow(list[i].Y - list[i + 1].Y, 2));

                xDistanceAvg += distance;
                xDistanceCount++;
            }

            xDistanceAvg /= xDistanceCount;

            Console.WriteLine("xDistanceAvg" + " : " + xDistanceAvg);

            // QuickSort(ref list, 0, list.Count);

            if (sorted != null)
            {
                Array.Clear(sorted, 0, sorted.Length);
                sorted = Array.Empty<StructHole>();
            }

            sorted = new StructHole[list.Count];

            try
            {
                MergeSort(ref list, 0, list.Count - 1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //리스트에서 정렬된 포인트를 제외하면서 한 컬럼씩 추출
            for (int i = 0; i < list.Count; i++)
            {
                //기준 포인트 설정
                StructHole pibot = list[0];

                list.RemoveAt(i);
                i--;
                int collectSize = 1;
                List<StructHole> collectList = new List<StructHole>();

                collectList.Add(pibot);
                //리스트에서 피봇과 같은 높이에 있는 포인트 추출

                for (int j = 0; j < list.Count; j++)
                {
                    double distance = Math.Sqrt(Math.Pow(pibot.X - list[j].X, 2) + Math.Pow(pibot.Y - list[j].Y, 2));

                    if (distance > xDistanceAvg * 2)
                    {
                        continue;
                    }

                    //if(Math.Abs(pibot.Y - tempPointList[j].Y) < 0.8)
                    double degree = Math.Atan2(Math.Abs(list[j].Y - pibot.Y), Math.Abs(list[j].X - pibot.X)) * (180.0 / Math.PI);

                    if (-range <= degree && range >= degree)
                    //if (degree >= 179 && degree <= 181 || degree <= 1 && degree >= -1 || degree <= -179 && degree >= -181)
                    //if (degree >= 178 && degree <= 182 || degree <= 2 && degree >= -2 || degree <= -178 && degree >= -182)
                    {
                        pibot = list[j];

                        collectSize++;

                        collectList.Add(list[j]);
                        list.RemoveAt(j);
                        j--;
                    }
                }

                for (int z = 0; z < collectList.Count; z++)
                {
                    pibot = collectList[z];

                    for (int t = 0; t < list.Count; t++)
                    {
                        double distance = Math.Sqrt(Math.Pow(pibot.X - list[t].X, 2) + Math.Pow(pibot.Y - list[t].Y, 2));

                        if (distance > xDistanceAvg * 2)
                        {
                            continue;
                        }

                        //if(Math.Abs(pibot.Y - tempPointList[j].Y) < 0.8)
                        double degree = Math.Atan2(Math.Abs(list[t].Y - pibot.Y), Math.Abs(list[t].X - pibot.X)) * (180.0 / Math.PI);
                        if (-range <= degree && range >= degree)
                        {
                            collectSize++;

                            collectList.Add(list[t]);
                            list.RemoveAt(t);
                            t--;
                        }
                    }
                }

                if (reverse)
                {
                    // collectList = collectList.OrderBy(point => point.X).ToList();
                    collectList = collectList.OrderByDescending(point => point.X).ToList();
                }
                else
                {
                    // collectList = collectList.OrderByDescending(point => point.X).ToList();
                    collectList = collectList.OrderBy(point => point.X).ToList();
                }

                column = 0;
                int index = 0;
                collectList.ForEach(p =>
                {
                    StructHole hp = new StructHole();
                    hp.Index = index++;
                    hp.X = p.X;
                    hp.Y = p.Y;
                    hp.VisionX = p.VisionX;
                    hp.VisionY = p.VisionY;
                    hp.Row = row;
                    hp.Column = column;
                    hp.IsCleaningFinish = p.IsCleaningFinish;
                    hp.IsOK = p.IsOK;

                    if (column % 3 == 1 && hp.AfterPoint != null && hp.AfterPoint.GroupIndex == hp.GroupIndex)
                    {
                        hp.IsTarget = true;
                    }

                    result.Add(hp);
                    column++;
                });

                reverse = !reverse;
                row++;
            }


            StructHole beforeHole = null;

            result.ForEach(hp =>
            {
                if (beforeHole != null)
                {
                    hp.BeforePoint = beforeHole;
                    beforeHole.AfterPoint = hp;
                }

                beforeHole = hp;
            });

            SortGroupIndex(result);
            double avg = 0;
            double minDistance = double.MaxValue;
            double maxDistance = 0;
            double avgCount = 0;
            result.ForEach(hp =>
            {
                if (hp.AfterPoint != null && hp.GroupIndex == hp.AfterPoint.GroupIndex)
                {
                    float distance = (float)Math.Sqrt(Math.Pow(hp.X - hp.AfterPoint.X, 2) + Math.Pow(hp.Y - hp.AfterPoint.Y, 2));
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                    }
                    if (maxDistance < distance)
                    {
                        maxDistance = distance;
                    }

                    avg += distance;
                    avgCount++;
                }
            });

            //거리계산
            avg /= avgCount;
            Console.WriteLine("max = " + maxDistance);
            Console.WriteLine("min = " + minDistance);
            Console.WriteLine("avg = " + avgCount);
            Console.WriteLine("avg = " + avg);

            result.ForEach(hp =>
            {
                if (hp.BeforePoint != null && hp.BeforePoint.GroupIndex == hp.GroupIndex)
                {
                    double degree = Math.Atan2(Math.Abs(hp.BeforePoint.Y - hp.Y), Math.Abs(hp.BeforePoint.X - hp.X)) * (180.0 / Math.PI);
                    float distance = (float)Math.Sqrt(Math.Pow(hp.X - hp.BeforePoint.X, 2) + Math.Pow(hp.Y - hp.BeforePoint.Y, 2));
                    if (-range <= degree && range >= degree)
                    {
                        if (Math.Abs(distance - avg) > minDistance / 3)
                        {
                            hp.IsSortStartPoint = true;
                        }
                        else
                        {
                            hp.IsSortStartPoint = false;
                        }


                    }

                    if (distance < filterRangeMin)
                    {
                        hp.IsSortStartPoint = false;
                    }
                    if (distance > filterRangeMax)
                    {
                        hp.IsSortStartPoint = true;
                    }

                    hp.AfterDistance = distance;
                }
            });

            SortGroupIndex(result);

            //Target 재 설정
            for (int i = 0; i < result.Count; i++)
            {
                StructHole hole = result[i];

                if (hole.Row == 4)
                {

                }

                if (hole.Column % 3 == 1 && hole.AfterPoint != null && hole.AfterPoint.GroupIndex == hole.GroupIndex)
                {
                    hole.IsTarget = true;
                }
                else
                {
                    hole.IsTarget = false;
                }

                if (hole.AfterPoint != null && hole.GroupIndex != hole.AfterPoint.GroupIndex)
                {
                    hole.IsTarget = false;
                }
            }

            return result;
        }

        private static void SortGroupIndex(List<StructHole> list)
        {
            //그룹 인덱스 설정
            int currentRow = 0;
            int currentGroupIndex = 0;
            for (int i = 0; i < list.Count; i++)
            {
                StructHole hole = list[i];
                if (currentRow != hole.Row)
                {
                    currentRow = hole.Row;
                    currentGroupIndex++;
                }
                else if ((hole.BeforePoint != null && hole.BeforePoint.Row != hole.Row) || hole.IsSortStartPoint)
                {
                    currentGroupIndex++;
                }
                hole.GroupIndex = currentGroupIndex;
            }
        }

        public static List<StructHole> CheckHole(List<StructHole> list)
        {
            SortGroupIndex(list);

            int count = 0;
            bool doBeforeCheck = false;

            //끝 부분 처리
            list.ForEach(hp =>
            {
                if (hp.BeforePoint != null && hp.BeforePoint.GroupIndex != hp.GroupIndex)
                {
                    count = 0;
                }

                count++;
                if (count == 3)
                {
                    hp.BeforePoint.IsTarget = true;
                    hp.IsTarget = false;
                    hp.BeforePoint.BeforePoint.IsTarget = false;
                    doBeforeCheck = true;
                    count = 0;
                }
                else if (hp.BeforePoint != null && (hp.AfterPoint == null || hp.AfterPoint.GroupIndex != hp.GroupIndex) && hp.BeforePoint.GroupIndex == hp.GroupIndex)
                {
                    hp.BeforePoint.IsTarget = true;
                    hp.IsTarget = false;

                    count = 0;
                }
            });

            //3개 미만인 그룹 타겟 제거
            int currentGroupIndex = 0;
            count = 0;
            list.ForEach(hp =>
            {
                if (hp.AfterPoint == null || hp.AfterPoint.GroupIndex == currentGroupIndex)
                {
                    count++;
                }

                if (list.Where(x => x.GroupIndex == hp.GroupIndex).ToList().Count < 3)
                {
                    list.Where(j => j.GroupIndex == hp.GroupIndex).ToList().ForEach(y =>
                    {
                        y.IsTarget = false;
                    });
                }
            });

            return list;
        }
    }
}
