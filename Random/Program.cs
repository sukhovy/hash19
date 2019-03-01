using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hash19
{
    class Program
    {

        enum Orientation
        {
            H,
            V
        }

        const string sample = "e";

        const string inputFile = @"D:\" + sample + ".txt";

        const string outputFile = @"D:\" + sample + "_result.txt";

        class PhotoIn
        {

            public Orientation orientation;

            public int num;

            public HashSet<string> tags;

            public HashSet<int> tags_int;

            public override string ToString()
            {
                return $"{orientation}, [{string.Join(",", tags)}]";
            }
        }

        class Result
        {
            public PhotoIn first;

            public PhotoIn second;

            private HashSet<int> Tags()
            {
                if (first.orientation == Orientation.H)
                {
                    return first.tags_int;
                }
                else
                {
                    return new HashSet<int>(first.tags_int.Concat(second.tags_int));
                }
            }

            public int Score(Result other)
            {
                var selfTags = Tags();
                var otherTags = other.Tags();

                var common = selfTags.Count(otherTags.Contains);
                var s1Tags = selfTags.Count(t => !otherTags.Contains(t));
                var s2Tags = otherTags.Count(t => !selfTags.Contains(t));

                return Math.Min(common, Math.Min(s1Tags, s2Tags));
            }
        }

        static void Main(string[] args)
        {
            string text;
            var fileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.ASCII))
            {
                text = streamReader.ReadToEnd();
            }

            var lines = text.Split('\n');

            var photosNum = int.Parse(lines[0]);

            var input = new PhotoIn[photosNum];

            for (var i = 0; i < photosNum; i++)
            {
                var photo = new PhotoIn();
                var photoData = lines[i + 1].Split();
                photo.orientation = (Orientation)Enum.Parse(typeof(Orientation), photoData[0]);

                var tagsNum = int.Parse(photoData[1]);
                photo.tags = new HashSet<string>();

                for (var j = 0; j < tagsNum; j++)
                {
                    photo.tags.Add(photoData[j + 2]);
                }

                photo.num = i;

                input[i] = photo;
            }

            var inputSize = input.Length;
            var random = new Random();

            for (int i = 0; i < input.Length * 1.5; i++)
            {
                var sourcePos = random.Next(inputSize);
                var destPos = random.Next(inputSize);

                var source = input[sourcePos];
                var dest = input[destPos];
                input[sourcePos] = dest;
                input[destPos] = source;

            }

            var intMapping = new Dictionary<string, int>();
            var currentId = 0;

            foreach (var photo in input)
            {
                photo.tags_int = new HashSet<int>();

                foreach (var tag in photo.tags)
                {
                    if (intMapping.TryGetValue(tag, out var value))
                    {
                        photo.tags_int.Add(value);
                    }
                    else
                    {
                        currentId++;
                        intMapping.Add(tag, currentId);
                        photo.tags_int.Add(currentId);
                    }
                }

                photo.tags = null;
            }

            var result = new List<Result>();

            var vertical = input.Where(x => x.orientation == Orientation.V).ToList();


            var horizontal = input.Where(x => x.orientation == Orientation.H).ToList();

            for (var i = 0; i < vertical.Count / 2; i++)
            {
                var resultItem = new Result()
                {
                    first = vertical[i * 2],
                    second = vertical[i * 2 + 1]
                };

                result.Add(resultItem);
            }

            for (var i = 0; i < horizontal.Count; i++)
            {
                var resultItem = new Result()
                {
                    first = horizontal[i],
                };

                result.Add(resultItem);
            }



            var size = result.Count;


            var score = 0;

            for (var i = 0; i < result.Count - 1; i++)
            {
                score += result[i].Score(result[i + 1]);
            }

            Console.WriteLine($"Start score: {score}");

            var currentPercent = 0;
            var prevPercent = -1;

            var iterations = 1_000_000;


            for (int i = 0; i < iterations; i++)
            {
                currentPercent = i * 100 / iterations;
                if (currentPercent != prevPercent)
                {
                    prevPercent = currentPercent;
                    Console.WriteLine($"done: {currentPercent}%");
                }

                var sourcePos = random.Next(size);
                var destPos = random.Next(size);

                var sourceCurrScore = 0;
                if (sourcePos > 0)
                {
                    sourceCurrScore += result[sourcePos].Score(result[sourcePos - 1]);
                }

                if (sourcePos < size - 1)
                {
                    sourceCurrScore += result[sourcePos].Score(result[sourcePos + 1]);
                }

                var destCurrScore = 0;
                if (destPos > 0)
                {
                    destCurrScore += result[destPos].Score(result[destPos - 1]);
                }

                if (destPos < size - 1)
                {
                    destCurrScore += result[destPos].Score(result[destPos + 1]);
                }

                var currentPairScore = sourceCurrScore + destCurrScore;

                var allVertical = new PhotoIn[4];

                var allVerticalBestPos = new PhotoIn[4];
                var bestVScore = 0;

                if (result[sourcePos].first.orientation == Orientation.V
                    && result[destPos].first.orientation == Orientation.V)
                {
                    allVertical[0] = result[sourcePos].first;
                    allVertical[1] = result[sourcePos].second;
                    allVertical[2] = result[destPos].first;
                    allVertical[3] = result[destPos].second;

                    for (var i1 = 0; i1 < 4; i1++)
                    {
                        for (var i2 = 0; i2 < 4; i2++)
                        {

                            if (i2 == i1)
                            {
                                continue;
                            }

                            for (var i3 = 0; i3 < 4; i3++)
                            {

                                if (i3 == i2 || i3 == i1)
                                {
                                    continue;
                                }

                                for (var i4 = 0; i4 < 4; i4++)
                                {

                                    if (i4 == i1 || i4 == i2 || i4 == i3)
                                    {
                                        continue;
                                    }

                                    var source = new Result()
                                    {
                                        first = allVertical[i1],
                                        second = allVertical[i2],
                                    };

                                    var dest = new Result()
                                    {
                                        first = allVertical[i3],
                                        second = allVertical[i4],
                                    };

                                    var sourceVScore = 0;
                                    if (sourcePos > 0)
                                    {
                                        sourceVScore += source.Score(result[sourcePos - 1]);
                                    }

                                    if (sourcePos < size - 1)
                                    {
                                        sourceVScore += source.Score(result[sourcePos + 1]);
                                    }

                                    var destVScore = 0;
                                    if (destPos > 0)
                                    {
                                        destVScore += dest.Score(result[destPos - 1]);
                                    }

                                    if (destPos < size - 1)
                                    {
                                        destVScore += dest.Score(result[destPos + 1]);
                                    }

                                    if (sourceVScore + destVScore > bestVScore)
                                    {
                                        bestVScore = sourceVScore + destVScore;
                                        allVerticalBestPos[0] = allVertical[i1];
                                        allVerticalBestPos[1] = allVertical[i2];
                                        allVerticalBestPos[2] = allVertical[i3];
                                        allVerticalBestPos[3] = allVertical[i4];
                                    }
                                }
                            }
                        }
                    }

                    if (bestVScore > currentPairScore)
                    {
                        result[sourcePos].first = allVerticalBestPos[0];
                        result[sourcePos].second = allVerticalBestPos[1];
                        result[destPos].first = allVerticalBestPos[2];
                        result[destPos].second = allVerticalBestPos[3];
                    }

                    continue;
                }

                var sourceFutureScore = 0;
                if (sourcePos > 0)
                {
                    sourceFutureScore += result[destPos].Score(result[sourcePos - 1]);
                }

                if (sourcePos < size - 1)
                {
                    sourceFutureScore += result[destPos].Score(result[sourcePos + 1]);
                }

                var destFutureScore = 0;
                if (destPos > 0)
                {
                    destFutureScore += result[sourcePos].Score(result[destPos - 1]);
                }

                if (destPos < size - 1)
                {
                    destFutureScore += result[sourcePos].Score(result[destPos + 1]);
                }

                var futurePairScore = sourceFutureScore + destFutureScore;


                if (futurePairScore > currentPairScore)
                {
                    var source = result[sourcePos];
                    result[sourcePos] = result[destPos];
                    result[destPos] = source;
                }

            }

            score = 0;

            for (var i = 0; i < result.Count - 1; i++)
            {
                score += result[i].Score(result[i + 1]);
            }

            Console.WriteLine($"Finish score: {score}");

            var output = new StringBuilder();

            output.Append(result.Count.ToString() + "\n");

            foreach (var item in result)
            {
                if (item.first.orientation == Orientation.H)
                {
                    output.Append(item.first.num + "\n");
                }
                else
                {
                    output.Append(item.first.num + " " + item.second.num + "\n");
                }
            }

            File.WriteAllText(outputFile, output.ToString());

            Console.WriteLine("Finished!");
            Console.Read();
        }
    }
}
