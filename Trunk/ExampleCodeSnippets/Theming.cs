
namespace ExampleCodeSnippets
{
    public class SymbolRotationTheming
    {
        /// <summary>
        /// Name of the column, which contains the rotation angle in degrees [0�, 360�]
        /// </summary>
        public System.String StyleRotationColumn { get; set; }

        /// <summary>
        /// Default vector style from which parts are to be modified
        /// </summary>
        public SharpMap.Styles.VectorStyle DefaultStyle { get; set; }

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="styleRotationColumn">Name of the column, which contains the rotation angle in degrees [0�, 360�]</param>
        public SymbolRotationTheming(System.String styleRotationColumn)
            :this(styleRotationColumn, new SharpMap.Styles.VectorStyle())
        {
        }

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="styleRotationColumn">Name of the column, which contains the rotation angle in degrees [0�, 360�]</param>
        /// <param name="defaultStyle">Default vector style from which parts are to be modified</param>
        public SymbolRotationTheming(System.String styleRotationColumn, SharpMap.Styles.VectorStyle defaultStyle)
        {
            StyleRotationColumn = styleRotationColumn;
            DefaultStyle = defaultStyle;
        }

        private static SharpMap.Styles.VectorStyle CloneStyle(SharpMap.Styles.VectorStyle styleToClone)
        {
            SharpMap.Styles.VectorStyle style =
                new SharpMap.Styles.VectorStyle
                    {
                        Enabled = styleToClone.Enabled,
                        MinVisible = styleToClone.MinVisible,
                        MaxVisible = styleToClone.MaxVisible,
                        Line = styleToClone.Line.Clone() as System.Drawing.Pen,
                        Fill = styleToClone.Fill.Clone() as System.Drawing.Brush,
                        Outline = styleToClone.Outline.Clone() as System.Drawing.Pen,
                        EnableOutline = styleToClone.EnableOutline,
                        Symbol = styleToClone.Symbol.Clone() as System.Drawing.Bitmap,
                        SymbolOffset = styleToClone.SymbolOffset,
                        SymbolRotation = styleToClone.SymbolRotation,
                        SymbolScale = styleToClone.SymbolScale
                    };

            return style;
        }

        public SharpMap.Styles.VectorStyle GetRotatedSymol(SharpMap.Data.FeatureDataRow row)
        {
            if (!System.String.IsNullOrEmpty(StyleRotationColumn))
                try
                {
                    SharpMap.Styles.VectorStyle dataStyle = CloneStyle(DefaultStyle);
                    dataStyle.SymbolRotation = System.Convert.ToSingle(row[StyleRotationColumn]);
                    return dataStyle;
                }
                catch { }

            return null;
        }
    }

    [NUnit.Framework.TestFixture]
    public class SymbolRotationThemingTest
    {
        private static readonly System.Random _randomNumberGenerator = new System.Random();
        static System.Double[] GetRandomOrdinates(System.Int32 size, System.Double min, System.Double max)
        {
            System.Double[] arr = new System.Double[size];
            System.Double width = max - min;
            for(System.Int32 i = 0; i < size; i++)
            {
                System.Double randomValue = _randomNumberGenerator.NextDouble();
                arr[i] = min + randomValue*width;
            }
            return arr;
        }
        
        [NUnit.Framework.Test]
        public void TestSymbolRotationTheming()
        {
            //Create a map
            SharpMap.Map map = new SharpMap.Map(new System.Drawing.Size(720,360));
            
            //Create some random sample data
            CreatingData cd = new CreatingData();
            SharpMap.Data.FeatureDataTable fdt =
                cd.CreatePointFeatureDataTableFromArrays(GetRandomOrdinates(80, -180, 180),
                                                         GetRandomOrdinates(80, -90, 90), null);

            //Add rotation column and fill with random rotation values
            fdt.Columns.Add("Rotation", typeof (System.Double));
            foreach (SharpMap.Data.FeatureDataRow row in fdt.Rows)
                row["Rotation"] = _randomNumberGenerator.NextDouble()*360d;


            //Create layer and datasource
            SharpMap.Layers.VectorLayer vl = new SharpMap.Layers.VectorLayer("Points", new SharpMap.Data.Providers.GeometryFeatureProvider(fdt));
            
            //Create default style
            SharpMap.Styles.VectorStyle defaultStyle = new SharpMap.Styles.VectorStyle();
            defaultStyle.Symbol = new System.Drawing.Bitmap(@"..\..\..\DemoWinForm\Resources\flag.png");
            defaultStyle.SymbolScale = 0.5f;

            //Create theming class and apply to layer
            SymbolRotationTheming srt = new SymbolRotationTheming("Rotation", defaultStyle);
            vl.Theme = new SharpMap.Rendering.Thematics.CustomTheme(srt.GetRotatedSymol);

            map.Layers.Add(vl);
            map.ZoomToExtents();
            System.Drawing.Image mapImage = map.GetMap();
            mapImage.Save("SymbolRotation.bmp");
        }
    }

}