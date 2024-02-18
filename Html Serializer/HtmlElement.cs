using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Html_Serializer
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class HtmlElement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();
        public List<string> Classes { get; set; } = new List<string>();
        public string InnerHtml { get; set; } = "";

        public HtmlElement Parent { get; set; }
        public List<HtmlElement> Children { get; set; } = new List<HtmlElement>();

        public HtmlElement(string name)
        {
            Name = name;
        }

        public static HtmlElement BuildTreeFromHtml(List<string> htmlStrings)
        {
            HtmlElement root = new HtmlElement("");
            HtmlElement currentElement = root;

            foreach (string htmlString in htmlStrings)
            {
              
                string firstWord = GetFirstWord(htmlString);

                if (firstWord.StartsWith("html/"))
                {
                    break;
                }

                if (firstWord.StartsWith("/"))
                {
                    if (currentElement.Parent != null) // Make sure there is a valid parent
                    {
                        currentElement = currentElement.Parent; // Go to previous level in the tree
                    }
                }
                else if (HtmlHelper.Instance.AllHtmlTags.Contains(firstWord)/*||flag*/)
                {

                    HtmlElement newElement = new HtmlElement(firstWord);
                    //{
                    //    Parent = currentElement
                    //};

                    var restOfString = htmlString.Remove(0, firstWord.Length);
                    var attributes = Regex.Matches(restOfString, "([a-zA-Z]+)=\\\"([^\\\"]*)\\\"")
                        .Cast<Match>()
                        .Select(m => $"{m.Groups[1].Value}=\"{m.Groups[2].Value}\"")
                        .ToList();

                    if (attributes.Any(attr => attr.StartsWith("class")))
                    {
                        // Handle class attribute
                        var classAttr = attributes.First(attr => attr.StartsWith("class"));
                        var classes = classAttr.Split('=')[1].Trim('"').Split(' ');
                        newElement.Classes.AddRange(classes);
                    }

                    newElement.Attributes.AddRange(attributes);


                    var idAttribute = attributes.FirstOrDefault(attr => attr.StartsWith("id"));
                    if (!string.IsNullOrEmpty(idAttribute))
                    {
                        newElement.Id = idAttribute.Split('=')[1].Trim('"');
                    }

                    newElement.Parent = currentElement;
                    currentElement.Children.Add(newElement);


                    if (htmlString.EndsWith("/") || HtmlHelper.Instance.SelfClosingTags.Contains(firstWord))
                    {
                        currentElement = newElement.Parent;
                    }
                    else
                    {
                          HtmlElement copyEl = currentElement;
                        newElement.Parent = copyEl;
                        currentElement = newElement;
                    }

                }
                else
                {
                    // Text content
                    currentElement.InnerHtml = htmlString;
                }
            }

            return root;
        }

        private static string GetFirstWord(string input)
        {
            return input.Split(' ').First().Trim();
        }




      public void PrintTree(int depth = 0, int maxDepth = 5)
        {
            if (depth > maxDepth)
            {
                /* Console.WriteLine("...");*/  // תוספת: יוסיף נקודות לדיור את סיום העץ
                return;
            }
            string indent = new string(' ', depth * 2);
            Console.WriteLine($" {indent}Name: {Name}");
            Console.WriteLine($"{indent}  Id: {Id}");
            Console.WriteLine($"{indent}  Attributes:");
            foreach (var attribute in Attributes)
            {
                Console.WriteLine($"{indent}    {attribute}");
            }
            Console.WriteLine($"{indent}  Classes: {string.Join(", ", Classes)}");
            Console.WriteLine($"{indent}  InnerHtml: {InnerHtml}");
            foreach (var child in Children)
            {
                child.PrintTree(depth + 1, maxDepth);
            }
            //Console.WriteLine($" {indent}/{Name}");
        }



        //2
        public IEnumerable<HtmlElement> Descendants()
        {
            Queue<HtmlElement> queue = new Queue<HtmlElement>();
            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                HtmlElement current = queue.Dequeue();
                yield return current;
                if (current.Children != null)
                {
                    foreach (var child in current.Children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }


        public IEnumerable<HtmlElement> Ancestors()
        {
            HtmlElement current = this;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }

        public List<HtmlElement> FindElements(Selector selector)
        {
            List<HtmlElement> searchResult = new List<HtmlElement>();
            HashSet<HtmlElement> searchSet = new HashSet<HtmlElement>();

            FindElementsRecursive(this, selector, searchResult, searchSet);

            return searchResult;
        }

        private void FindElementsRecursive(HtmlElement currentElement, Selector currentSelector, List<HtmlElement> searchResult, HashSet<HtmlElement> searchSet)
        {
            foreach (var descendant in currentElement.Descendants())
            {
                if (MeetsSelectorCriteria(descendant, currentSelector) && searchSet.Add(descendant))
                {
                    if (currentSelector.Child == null)
                    {
                        searchResult.Add(descendant);
                    }
                    else
                    {
                        FindElementsRecursive(descendant, currentSelector.Child, searchResult, searchSet);
                    }
                }
            }
        }

        private bool MeetsSelectorCriteria(HtmlElement element, Selector selector)
        {
            // Check TagName
            if (!string.IsNullOrEmpty(selector.TagName) && !string.Equals(selector.TagName, element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check Id
            if (!string.IsNullOrEmpty(selector.Id) && !string.Equals(selector.Id, element.Id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check Classes
            foreach (var className in selector.Classes)
            {
                if (!element.Classes.Contains(className, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

    }

}



