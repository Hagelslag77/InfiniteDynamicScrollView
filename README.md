# Infinite Dynamic Scroll View

[![license](https://img.shields.io/badge/license-MIT-green.svg?style=flat&cacheSeconds=2592000)](https://github.com/Hagelslag77/InfiniteDynamicScrollView/blob/main/LICENSE)
[![Unity 6000.3.1f1+](https://img.shields.io/badge/Unity-6000.3.f1+-blue)](https://unity3d.com/get-unity/download/archive)

## What is Infinite Dynamic Scroll View

Infinite Dynamic Scroll View is yet another infinite scroll view for Unity. This means, only items visible in the viewport are instantiated, reducing memory usage and improving performance.
Infinite Dynamic Scroll View adds support for dynamic item sizes - a feature I wasn't able to find in any other scroll view.

## Features

* Dynamic GameObject sizes in the same Scroll View
* Supports any number of GameObjects
* Support for different GameObjects based on the data provided
* Supports custom Dependency Injection
* Supports custom Object Pools

As this is - for the time being - a pretty new project please have a look at [Limitations](#limitations) below before using it in your project.

## Installation

### Unity Package Manager

#### Using the Package Manager Window

In Unity select [Window -> Package Management -> Package Manager](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-ui.html).

Click the + in the top left corner and select Add package from git URL.

Enter ``https://github.com/Hagelslag77/InfiniteDynamicScrollView.git#latest`` in the dialog.

alternatively, you can also add the package manually as described below.

#### Edit manifest.json

Open the file ``Packages/manifest.json`` in a text editor and add the following line to the dependencies section:

```json
{
  "dependencies": {
    ....
    "hagelslag.infinitedynamicscrollview": "https://github.com/Hagelslag77/InfiniteDynamicScrollView.git#latest"
  }
}
```

### Pin the Package to a specific version

In case you do not want to use the latest version of the package, check the [git tags](https://github.com/Hagelslag77/InfiniteDynamicScrollView/tags) 
for the version you want to use and replace ``latest`` with the version number (e.g. ``https://github.com/Hagelslag77/InfiniteDynamicScrollView.git#1.0.0``).

## Usage

Infinite Dynamic Scroll View uses two components: ``VerticalScrollView`` and ``VerticalCell``. ``VerticalScrollView`` is the content holder for the cells and ``VerticalCell`` is the actual instance that is instantiated for eacht data entry shown.
Both classes are generics that need to specialed for the type of data you want to use.

### VerticalCell

Derive your cell class from ``VerticalCell<TData>`` and implement the abstract methods.

``void SetData(TData data)`` is called to initialize the cell with the data. Cell instances are reused and ``SetData`` is called for each new data entry.

``float GetHeight(float width)`` should return the preferred height of the cell. The ``width`` parameter is the maximal width of the cell it is allowed use.

Example using a string as data:
```csharp
using UnityEngine;
using TMPro;
using Hagelslag.InfiniteDynamicScrollView;

public class SimpleScrollCell : VerticalCell<string>
{
    [SerializeField] private TextMeshProUGUI m_text;

    public override void SetData(string text) => m_text.text = text;

    public override float GetHeight(float width)
    {
        return m_text.GetPreferredValues(width, Mathf.Infinity).y;
    }
}
```

### VerticalScrollView

Derive your scroll view class from ``VerticalScrollView<TData>``. Usually the implemenation can be left empty, since the derived class is need only to specify the data type ``TData``.

You might want to override the ``VerticalCell<TData> InstantiateCell(TData data, Transform parent)`` method to use a custom Dependency Injection container.

In order to use your own custom ObjectPool override ``IObjectPool<TData> ObjectPool``.

Example using a string as data:
```csharp
using Hagelslag.InfiniteDynamicScrollView;

public sealed class SimpleScrollView : VerticalScrollView<string>
{}
```

## Author
[Hagelslag77](https://github.com/Hagelslag77/)

## Limitations

* Supports only vertical scrolling

## License

MIT License

Copyright © 2025 Hagelslag77
