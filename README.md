[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.BoxShadow/master/Shared/NuGet/Icon.png "Zebble.BoxShadow"


## Zebble.BoxShadow

![logo]

A plugin to add shadow for all objects in Zebble application.


[![NuGet](https://img.shields.io/nuget/v/Zebble.BoxShadow.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.BoxShadow/)

> With BoxShadow plugin you can add rounded and typical shadow with different color and offset in all of platforms of Zebble aplications.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.BoxShadow/](https://www.nuget.org/packages/Zebble.BoxShadow/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage
To add shadow to the Views you can use below code:
```csharp
public override async Task OnInitialized()
{
	myLabel.BoxShadow(xOffset: 0, yOffset: 0, blurRadius: 7, expand: -5, color: Colors.DarkGray);	
}
```

<br>

### Properties
| Property     | Type         | Android | iOS | Windows |
| :----------- | :----------- | :------ | :-- | :------ |
| Color           | Color          | x       | x   | x       |
| XOffset           | int          | x       | x   | x       |
| YOffset           | int          | x       | x   | x       |
| BlurRadius | int | x       | x   | x       |
| For | View | x       | x   | x       |
