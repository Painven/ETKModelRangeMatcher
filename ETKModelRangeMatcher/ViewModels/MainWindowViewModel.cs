using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ETKModelRangeMatcher.ViewModels;
public class MainWindowViewModel : ViewModelBase
{
    readonly string modelRangesFile = "model_ranges.txt";
    readonly string productsFile = "products.txt";
    readonly string ignoreProductsFile = "ignore.txt";

    string title = "ETK Model Range Matcher";
    public string Title
    {
        get => title;
        set => Set(ref title, value);
    }

    string selectPattern;
    public string SelectPattern
    {
        get => selectPattern;
        set
        {
            try
            {
                var testCheck = Regex.IsMatch("", value);
                Set(ref selectPattern, value);
            }
            catch
            {

            }
        }
    }
    string newModelRangeProductModel;
    public string NewModelRangeProductModel
    {
        get => newModelRangeProductModel;
        set => Set(ref newModelRangeProductModel, value);
    }

    string newModelRangeProductName;
    public string NewModelRangeProductName
    {
        get => newModelRangeProductName;
        set => Set(ref newModelRangeProductName, value);
    }

    public ICommand LoadSourceCommand { get; }
    public ICommand CreateModelRangeCommand { get; }
    public ICommand SelectByPatternCommand { get; }
    public ICommand GetModelRangesDescriptionCommand { get; }

    public ObservableCollection<ProductLine> IgnoredProducts { get; set; }
    public ObservableCollection<ProductLine> ProductLines { get; set; }
    public ObservableCollection<MatchedModelRange> MatchedModelRanges { get; set; }


    public MainWindowViewModel()
    {
        LoadSourceCommand = new LambdaCommand(LoadProductLines);
        CreateModelRangeCommand = new LambdaCommand(CreateNewModelRange);
        GetModelRangesDescriptionCommand = new LambdaCommand(GetModelRangesDescription);
        SelectByPatternCommand = new LambdaCommand(SelectByPattern, e => !string.IsNullOrWhiteSpace(SelectPattern));
        ProductLines = new ObservableCollection<ProductLine>();
        MatchedModelRanges = new ObservableCollection<MatchedModelRange>();
        IgnoredProducts = new ObservableCollection<ProductLine>();


        foreach (var file in new[] { modelRangesFile, productsFile, ignoreProductsFile })
        {
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "");
            }
        }

        ProductLines.CollectionChanged += ProductLines_CollectionChanged;
    }

    private void GetModelRangesDescription(object obj)
    {
        var dic = File.ReadAllLines("empty_mr_data.txt")
            .Select(line => line.Split('\t'))
            .ToDictionary(i => int.Parse(i[0]), i => i[1].Split(";").Select(pid => int.Parse(pid)).ToArray());
        var productsData = File.ReadAllLines("adjacent_names.txt")
            .Select(line => line.Split('\t'))
            .Select(i => new ProductData() { ProductId = int.Parse(i[0]), Sku = i[1], Name = i[2] })
            .ToArray();

        var data = SqlBuilder.GetModelRangesDescriptions(dic, productsData);

        Clipboard.SetText(data);

        MessageBox.Show("SQL вставлен в буфер обмена");
    }

    private void ProductLines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
        {
            int modelRangeCount = MatchedModelRanges.Sum(mr => mr.ChildCount);
            double progress = (double)(modelRangeCount + IgnoredProducts.Count) / ProductLines.Count;
            Title = $"Осталось {ProductLines.Count} ({progress:P0}) | Обьеденено: {modelRangeCount} | Игнор: {IgnoredProducts.Count}";
        }
    }

    private void SelectByPattern(object obj)
    {
        ProductLines.Where(p => p.IsChecked).ToList().ForEach(p => p.IsChecked = false);

        var matched = ProductLines
            .Where(p =>
                Regex.IsMatch(p.Name, SelectPattern, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(p.Model, SelectPattern, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(p.Sku, SelectPattern, RegexOptions.IgnoreCase))
            .ToList();

        if (matched.Any())
        {
            matched.ForEach(p => p.IsChecked = true);

            List<int> indexes = new List<int>();

            foreach (var m in matched)
            {
                indexes.Add(ProductLines.IndexOf(m));
            }
            string indexNumbers = string.Join(" | ", indexes);

            Title = $"Выделено: {matched.Count} со следующими индексами: ({indexNumbers})";
        }
        else
        {

            Title = "Нет совпадений";
        }

    }

    private void CreateNewModelRange(object obj)
    {
        var checkedProducts = ProductLines.Where(p => p.IsChecked);

        var modelRange = new MatchedModelRange()
        {
            Name = NewModelRangeProductName,
            Model = NewModelRangeProductModel,
            AdjacentIds = checkedProducts.Select(p => p.ProductId).ToList()
        };

        if (modelRange.AdjacentIds.Count == 0 || checkedProducts.Count() == 0)
        {
            MessageBox.Show("Список пуст");
            return;
        }

        MatchedModelRanges.Insert(0, modelRange);
        File.AppendAllLines(modelRangesFile, new string[] { modelRange.ToStringFormat() });

        foreach (var p in checkedProducts.ToArray())
        {
            ProductLines.Remove(p);
        }

        NewModelRangeProductModel = string.Empty;
        NewModelRangeProductName = string.Empty;

    }

    private void LoadProductLines(object obj)
    {
        LoadModelRanges();
        LoadProductLines();
    }

    private void LoadModelRanges()
    {

        MatchedModelRanges.Clear();
        var data = File.ReadAllLines(modelRangesFile)
            .Select(line => MatchedModelRange.Parse(line))
            .ToList();

        data.Reverse();
        data.ForEach(mr => MatchedModelRanges.Add(mr));
    }

    private void LoadProductLines()
    {
        string[] ignoredProducts = File.ReadAllLines(ignoreProductsFile).Distinct().ToArray();
        string[] alreadyAddedProducts = MatchedModelRanges.SelectMany(p => p.AdjacentIds).Distinct().ToArray();

        ProductLines.Clear();
        var parsedLines = File.ReadAllLines(productsFile)
            .Select(line => ProductLine.Parse(line))
            .Where(line => !alreadyAddedProducts.Contains(line.ProductId) && !ignoredProducts.Contains(line.ProductId))
            .ToArray();

        foreach (var line in parsedLines)
        {
            ProductLines.Add(line);
            line.OnRemoveClicked += () =>
            {
                IgnoredProducts.Add(line);
                ProductLines.Remove(line);
                File.AppendAllLines(ignoreProductsFile, new string[] { line.ProductId });
            };
        }
    }

}
