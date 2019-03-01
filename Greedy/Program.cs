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

        const string sample = "b";

        const string inputFile = @"D:\" + sample + ".txt";

        const string outputFile = @"D:\" + sample + "_result.txt";

        class Photo
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

        class Slide
        {
            public Photo first;

            public Photo second;

            private HashSet<int> Tags()
            {
                return Tags(first, second);
            }

            public static HashSet<int> Tags(Photo first, Photo second)
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

            public int Score(HashSet<int> otherTags)
            {
                var selfTags = Tags();

                var common = selfTags.Count(otherTags.Contains);
                var s1Tags = selfTags.Count - common;
                var s2Tags = otherTags.Count - common;

                return Math.Min(common, Math.Min(s1Tags, s2Tags));
            }

            public int Score(Slide other)
            {
                
                var otherTags = other.Tags();

                return Score(otherTags);
                
            }
        }

        class TmpSlide
        {
            public int score;

            public int photo1;

            public int photo2;
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

            var input = new Photo[photosNum];

            for (var i = 0; i < photosNum; i++)
            {
                var photo = new Photo();
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

            var result = new List<Slide>();

            var verticalList = input.Where(x => x.orientation == Orientation.V)
                .ToList();
            
            var horizontalList = input.Where(x => x.orientation == Orientation.H)
                .ToList();

            var vertical = new Dictionary<int, Photo>();
            var horizontal = new Dictionary<int, Photo>();

            for(var i = 0; i < verticalList.Count; i++)
            {
                vertical.Add(i, verticalList[i]);
            }

            for (var i = 0; i < horizontalList.Count; i++)
            {
                horizontal.Add(i, horizontalList[i]);
            }

            var total = horizontal.Count / 2 + vertical.Count;

            if (horizontal.Any())
            {
                var firstSlide = new Slide()
                {
                    first = horizontal[0]
                };

                result.Add(firstSlide);

                horizontal.Remove(0);
            }
            else
            {
                var firstSlide = new Slide()
                {
                    first = vertical[0],
                    second = vertical[1],
                };

                result.Add(firstSlide);

                vertical.Remove(0);
                vertical.Remove(1);
            }

            var score = 0;

            while (vertical.Any() || horizontal.Any())
            {
                var currentLastSlide = result[result.Count - 1];

                var hMax = horizontal.AsParallel()
                    .WithDegreeOfParallelism(12)
                    .Select(h => {

                    return new TmpSlide
                    {
                        score = currentLastSlide.Score(Slide.Tags(h.Value, null)),
                        photo1 = h.Key,
                    };
                })
                .OrderByDescending( x => x.score)
                .FirstOrDefault();
                
                var vMax = vertical.AsParallel()
                    .WithDegreeOfParallelism(12)
                    .SelectMany(v1 =>
                {
                    return vertical.Where(x => x.Key > v1.Key).Select(v2 => {

                        return new TmpSlide()
                        {
                            score = currentLastSlide.Score(Slide.Tags(v1.Value, v2.Value)),

                            photo1 = v1.Key,
                            photo2 = v2.Key,
                        };
                    });
                })
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

                var maxScore = 0;

                if((vMax != null && hMax == null) || (vMax != null && hMax != null && vMax.score > hMax.score))
                {                    
                    var newLastSlide = new Slide()
                    {
                        first = vertical[vMax.photo1],
                        second = vertical[vMax.photo2],
                    };

                    result.Add(newLastSlide);
                                        
                    vertical.Remove(vMax.photo1);
                    vertical.Remove(vMax.photo2);

                    maxScore = vMax.score;
                }
                else
                {
                    var newLastSlide = new Slide()
                    {
                        first = horizontal[hMax.photo1],
                    };

                    result.Add(newLastSlide);

                    horizontal.Remove(hMax.photo1);

                    maxScore = hMax.score;
                }

                score += maxScore;

                Console.WriteLine($"result size: {result.Count * 100 / total}%, score {score}");
                
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
