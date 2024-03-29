﻿using System;
using System.Windows.Input;

namespace ETKModelRangeMatcher.ViewModels;

public class ProductLine : ViewModelBase
{
    public event Action OnRemoveClicked;

    public string ProductId { get; set; }
    public string Model { get; set; }
    public string Sku { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }

    public ICommand RemoveItemCommand { get; set; }
    public ICommand ToggleCheckStateCommand { get; set; }

    public ProductLine()
    {
        RemoveItemCommand = new LambdaCommand(e => OnRemoveClicked?.Invoke());
        ToggleCheckStateCommand = new LambdaCommand(e => IsChecked = !IsChecked);
    }

    public string ImageFullPath => $"https://etk-komplekt.ru/image/{Image}";

    bool isChecked;
    public bool IsChecked
    {
        get => isChecked;
        set => Set(ref isChecked, value);
    }

    public static ProductLine Parse(string str)
    {
        var arr = str.Split(new string[] { "\t" }, System.StringSplitOptions.None);

        if (arr.Length == 5)
        {
            return new ProductLine()
            {
                ProductId = arr[0],
                Model = arr[1],
                Sku = arr[2],
                Name = arr[3],
                Image = arr[4]
            };
        }
        throw new FormatException(nameof(str));
    }
}
