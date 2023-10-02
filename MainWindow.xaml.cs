using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Tarea2.Classes;
using MathNet.Numerics.LinearAlgebra;

namespace Tarea2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RenderData renderData;

        Matrix<double> cameraMatrixTranspose;
        Matrix<double> cameraTransformMatrix;
        Matrix<double> projectionMatrix;

        double xDMax;
        double xDMin;
        double yDMax;
        double yDMin;

        List<Vector<double>> projectedPoints = new List<Vector<double>>();

        Dictionary<string, Vector<double>> drewPoints = new Dictionary<string, Vector<double>>();

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                renderData = LoatDataFromJson.LoadRenderDataFromJson("C:\\Users\\WINDOWS11\\Documents\\Apuntes IAV 7 Semestre\\ComputacionGrafica\\proyectoComputacionGrafica\\drawConfig.json");
            }
            catch
            {
                MessageBox.Show("El archivo JSON de configuración no ha sido encontrado", "Error");
                Environment.Exit(0);
            }

            cameraMatrixTranspose = Matrix<double>.Build.DenseOfRowVectors
            (
                renderData.UDirectorVector,
                renderData.VDirectionVector,
                renderData.WDirectionVector
            );

            cameraTransformMatrix = CalculateCameraTransformMatrix();

            projectionMatrix = CalculateProjectionMatrix();

            foreach (Vector<double> pointToRender in renderData.PointsToRender.Values)
            {
                Vector<double> tempCameraPoint = TransformWorldPointToCameraPoint(pointToRender);
                Vector<double> tempProjectedPoint = ProjectCameraPoint(tempCameraPoint);

                projectedPoints.Add(tempProjectedPoint);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Inicializamos los X y Y máximos y mínimos de nuestros dispositivo
            // Tiene que ser aqui cuando el canva ya existe
            (xDMin, xDMax, yDMin, yDMax) = GetCanvasSize();

           if (projectedPoints.Count > 0)
           {
                int i = 1;
                foreach (Vector<double> projectedPoint in projectedPoints) 
                {
                    var (x, y) = GetDevicePointFromProjection(projectedPoint);

                    Rectangle pixelToDraw = new Rectangle{ Fill = Brushes.White, Width = 1, Height = 1};

                    Canvas.SetLeft(pixelToDraw, x); Canvas.SetTop(pixelToDraw, y);

                    Device.Children.Add(pixelToDraw);

                    Vector<double> tempDrewPoint = Vector<double>.Build.DenseOfArray(new double[] {x, y});
                    drewPoints.Add(i.ToString(), tempDrewPoint);
                    i++;
                }
                
           }

           DrawAllLines();
           FillAllTriangles();
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            renderData.CameraPosition[1] = renderData.CameraPosition[1] + 1;
            ReExecuteRenderPipeline();
        }

        private void Left_Click(object sender, RoutedEventArgs e)
        {
            renderData.CameraPosition[0] = renderData.CameraPosition[0] - 1;
            ReExecuteRenderPipeline();
        }

        private void Right_Click(object sender, RoutedEventArgs e)
        {
            renderData.CameraPosition[0] = renderData.CameraPosition[0] + 1;
            ReExecuteRenderPipeline();
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            renderData.CameraPosition[1] = renderData.CameraPosition[1] - 1;
            ReExecuteRenderPipeline();
        }

        private Matrix<double> CalculateCameraTransformMatrix()
        {
            Matrix<double> cameraTransformMatrix = Matrix<double>.Build.DenseOfMatrix(cameraMatrixTranspose);

            // Calculando matrix de la -camaraTranspuesta * posición de la camara
            Vector<double> negCameraMatrix_X_CameraPosition = cameraMatrixTranspose.Multiply(renderData.CameraPosition).Multiply(-1);

            // Armando la matriz de la camara, primero aniadimos el resultado de arriba a la derecha de la matriz
            cameraTransformMatrix = cameraTransformMatrix.Append(negCameraMatrix_X_CameraPosition.ToColumnMatrix());
            // Luego agregamos el default de 0,0,0,1
            Vector<double> defaultVector = Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 0, 1 });
            cameraTransformMatrix = cameraTransformMatrix.Stack(defaultVector.ToRowMatrix());

            return cameraTransformMatrix;
        }

        private Matrix<double> CalculateProjectionMatrix()
        {   
            // Calculando los valores extranios, luego se pregunta al profe los nombres
            double fDividedW = renderData.f / renderData.w;
            double fDividedH = renderData.f / renderData.h;
            double notEvenBotheringNamingThis = (-1.0) * ( (renderData.l + renderData.c) / (renderData.l - renderData.c));
            double theSameWithThis = (-1.0) * ( 2.0 * (renderData.l * renderData.c) / (renderData.l - renderData.c) );

            // Armando las filas de la matriz
            Vector<double> row1 = Vector<double>.Build.DenseOfArray(new double[] { fDividedW, 0, 0, 0 });
            Vector<double> row2 = Vector<double>.Build.DenseOfArray(new double[] { 0, fDividedH, 0, 0 });
            Vector<double> row3 = Vector<double>.Build.DenseOfArray(new double[] { 0, 0, notEvenBotheringNamingThis, theSameWithThis });
            Vector<double> row4 = Vector<double>.Build.DenseOfArray(new double[] { 0, 0, -1, 0 });

            // Armando y devolviendo la matriz
            return Matrix<double>.Build.DenseOfRowVectors
            (
                row1,
                row2,
                row3,
                row4
            );
        }

        // Se va a inferir que esta funcion recibe un vector 1x3, internamente se le agrega el 1, para hacerlo 1x4
        // y que pueda ser multiplicado por la matriz de transformacion de la camara
        private Vector<double> TransformWorldPointToCameraPoint(Vector<double> worldPoint)
        {   
            // Agregamos el 1
            worldPoint = Vector<double>.Build.DenseOfEnumerable(worldPoint.AsEnumerable().Concat(new double[] { 1 }));
            Vector<double> newCameraPoint = cameraTransformMatrix.Multiply(worldPoint);

            // Hecho el calculo removemos el 1
            newCameraPoint = Vector<double>.Build.DenseOfEnumerable(newCameraPoint.AsEnumerable().Take(newCameraPoint.Count - 1));

            return newCameraPoint;
        }

        // Igual se infiere que viene un vector 1x3, y se va a devolver un vector 1x2
        private Vector<double> ProjectCameraPoint(Vector<double> cameraPoint)
        {
            // Agregamos el 1
            cameraPoint = Vector<double>.Build.DenseOfEnumerable(cameraPoint.AsEnumerable().Concat(new double[] { 1 }));
            Vector<double> newProjectedPoint = projectionMatrix.Multiply(cameraPoint);

            // Es muy probable que el elemento homogeneo del vector no sea 1, por lo que tenemos dividir todo entre este
            // ultimo elemento para forzarlo a ser 1
            double lastElement = newProjectedPoint[newProjectedPoint.Count - 1];
            newProjectedPoint = newProjectedPoint.Map(number => number / lastElement);

            // Hecho el calculo removemos el ultimo elemento, y quitamos la coordenada Z, ya que pasamos a 2D
            newProjectedPoint = Vector<double>.Build.DenseOfEnumerable(newProjectedPoint.AsEnumerable().Take(newProjectedPoint.Count - 1));
            newProjectedPoint = Vector<double>.Build.DenseOfEnumerable(newProjectedPoint.AsEnumerable().Take(newProjectedPoint.Count - 1));

            return newProjectedPoint;
        }

        // Recibe un vector 1x2 con las coordenadas proyectadas y devuelve
        // el pixel a pintar en el dispositivo ya redondeado
        private Tuple<int, int> GetDevicePointFromProjection(Vector<double> projectedPoint)
        {
            // Sacamos Xp y Yp del punto pasado por parametro
            var (Xp, Yp) = (projectedPoint[0], projectedPoint[1]);

            double firstXCalculaionPart = (xDMax - xDMin) / (renderData.xPMax - renderData.xPMin) * Xp;
            double secondXCalculaionPart = (xDMax - xDMin) / (renderData.xPMax - renderData.xPMin) * (renderData.xPMin * -1);
            double xD = firstXCalculaionPart + secondXCalculaionPart + xDMin;

            double firstYCalculaionPart = (yDMax - yDMin) / (renderData.yPmax - renderData.yPMin) * Yp;
            double secondYCalculaionPart = (yDMax - yDMin) / (renderData.yPmax - renderData.yPMin) * (renderData.yPMin * -1);
            double yD = firstYCalculaionPart + secondYCalculaionPart + yDMin;

            // Restamos 0.5 y redondeamos al numero entero mas proximo
            xD = Math.Round(xD - 0.5);
            yD = Math.Round(yD - 0.5);

            return Tuple.Create((int)xD, (int)yD);
        }

        private void DrawLine(Vector<double> pointFrom, Vector<double> pointTo)
        {
            // Lo convertimos a int para evitar decimales
            var (x1, y1) = ((int)pointFrom[0], (int)pointFrom[1]);
            var (x2, y2) = ((int)pointTo[0], (int)pointTo[1]);

            // Calculamos mx y my
            int mx = x2 - x1;
            int my = y2 - y1;

            // Elejimos la "s", lo creamos como double para que al calcular los deltas no se coma decimales
            double s = Math.Max(Math.Abs(mx), Math.Abs(my));
            
            double deltaX = mx / s;
            double deltaY = my / s;

            double lastBx = x1;
            double lastBy = y1;
            // Pintamos la linea las veces que lo indique "s", lo usamos en int para evitar problemas
            for (int i = 0; i < (int)s; i++)
            {
                lastBx = lastBx + deltaX;
                lastBy = lastBy + deltaY;

                Rectangle pixelToDraw = new Rectangle { Fill = Brushes.White, Width = 1, Height = 1 };

                int newX = (int)Math.Round(lastBx);
                int newY = (int)Math.Round(lastBy);

                Canvas.SetLeft(pixelToDraw, newX); Canvas.SetTop(pixelToDraw, newY);

                Device.Children.Add(pixelToDraw);
            }
        }

        private void FillTriangle(Vector<double> point1, Vector<double> point2, Vector<double> point3)
        {
            // Buscamos cual es el punto mas arriba en coordenadas de Y, esto facilita el algoritmo, pues pintamos de arriba hacia abajo
            // Lo nos ayuda facilita calcular la interpolacion para la linea a dibujar, y por lo tanto nos aseguramos que todo el triangulo se rellene
            List<Vector<double>> points = new List<Vector<double>> { point1, point2, point3 };
            points.Sort((a, b) => a[1].CompareTo(b[1]));

            Vector<double> topPoint = points[0];
            Vector<double> middlePoint = points[1];
            Vector<double> bottomPoint = points[2];

            // Rellenamos el triangulo
            for (int y = (int)topPoint[1]; y <= (int)bottomPoint[1]; y++)
            {
                // Interpolamos para conocer el valor de X
                if (y < middlePoint[1])
                {
                    DrawLine(Vector<double>.Build.DenseOfArray(new double[] { Interpolate(topPoint[0], middlePoint[0], y, topPoint[1], middlePoint[1]), y }),
                              Vector<double>.Build.DenseOfArray(new double[] { Interpolate(topPoint[0], bottomPoint[0], y, topPoint[1], bottomPoint[1]), y }));
                }
                else
                {
                    DrawLine(Vector<double>.Build.DenseOfArray(new double[] { Interpolate(middlePoint[0], bottomPoint[0], y, middlePoint[1], bottomPoint[1]), y }),
                              Vector<double>.Build.DenseOfArray(new double[] { Interpolate(topPoint[0], bottomPoint[0], y, topPoint[1], bottomPoint[1]), y }));
                }
            }
        }

        private double Interpolate(double x1, double x2, double y, double y1, double y2)
        {
            if (y1 == y2) return x1;
            return ((y - y1) / (y2 - y1)) * (x2 - x1) + x1;
        }

        private void DrawAllLines()
        {
            // ESTOS PUNTOS ESTAN HARDCODEADOS
            // Cara inferior
            DrawLine(drewPoints["1"], drewPoints["2"]);
            DrawLine(drewPoints["1"], drewPoints["8"]);
            DrawLine(drewPoints["2"], drewPoints["7"]);
            DrawLine(drewPoints["7"], drewPoints["8"]);

            // Cara superior
            DrawLine(drewPoints["3"], drewPoints["4"]);
            DrawLine(drewPoints["3"], drewPoints["6"]);
            DrawLine(drewPoints["5"], drewPoints["4"]);
            DrawLine(drewPoints["5"], drewPoints["6"]);

            // Cara Frontal
            DrawLine(drewPoints["1"], drewPoints["3"]);
            DrawLine(drewPoints["8"], drewPoints["6"]);

            // Cara Trasera
            DrawLine(drewPoints["2"], drewPoints["4"]);
            DrawLine(drewPoints["7"], drewPoints["5"]);
        }

        private void FillAllTriangles()
        {
            // IGUAL TRIANGULOS HARCODEADOS, SON UN TOTAL DE 12

            // Triangulos de atras
            FillTriangle(drewPoints["2"], drewPoints["4"], drewPoints["5"]);
            FillTriangle(drewPoints["2"], drewPoints["7"], drewPoints["5"]);

            // Parte delantera
            FillTriangle(drewPoints["1"], drewPoints["8"], drewPoints["6"]);
            FillTriangle(drewPoints["1"], drewPoints["3"], drewPoints["6"]);

            // Parte arriba
            FillTriangle(drewPoints["3"], drewPoints["4"], drewPoints["5"]);
            FillTriangle(drewPoints["3"], drewPoints["6"], drewPoints["5"]);

            // Parte abajo
            FillTriangle(drewPoints["1"], drewPoints["2"], drewPoints["7"]);
            FillTriangle(drewPoints["1"], drewPoints["8"], drewPoints["7"]);

            // Parte derecha
            FillTriangle(drewPoints["7"], drewPoints["5"], drewPoints["6"]);
            FillTriangle(drewPoints["7"], drewPoints["8"], drewPoints["6"]);

            // Parte Izquierda
            FillTriangle(drewPoints["2"], drewPoints["4"], drewPoints["3"]);
            FillTriangle(drewPoints["2"], drewPoints["1"], drewPoints["3"]);
        }

        private Tuple<double, double, double, double> GetCanvasSize()
        {
            double xDMax = Device.ActualWidth;
            double xDMin = 1;

            // El origen del canvas de WPF se pinta desde arriba a la derecha, por lo que yDMax y yDMin se invierten.
            double yDMax = 1;
            double yDMin = Device.ActualHeight;

            return Tuple.Create(xDMin, xDMax, yDMin, yDMax);
        }

        private void ReExecuteRenderPipeline()
        {   
            // Tenemos que volver a calcular la matriz de la camara, porque la posicion cambio
            cameraTransformMatrix = CalculateCameraTransformMatrix();

            projectedPoints.Clear();

            foreach (Vector<double> pointToRender in renderData.PointsToRender.Values)
            {
                Vector<double> tempCameraPoint = TransformWorldPointToCameraPoint(pointToRender);
                Vector<double> tempProjectedPoint = ProjectCameraPoint(tempCameraPoint);

                projectedPoints.Add(tempProjectedPoint);
            }

            if (projectedPoints.Count > 0)
            {
                Device.Children.Clear();
                drewPoints.Clear();

                int i = 1;
                foreach (Vector<double> projectedPoint in projectedPoints)
                {
                    var (x, y) = GetDevicePointFromProjection(projectedPoint);

                    Rectangle pixelToDraw = new Rectangle { Fill = Brushes.White, Width = 1, Height = 1 };

                    Canvas.SetLeft(pixelToDraw, x); Canvas.SetTop(pixelToDraw, y);

                    Device.Children.Add(pixelToDraw);

                    Vector<double> tempDrewPoint = Vector<double>.Build.DenseOfArray(new double[] { x, y });
                    drewPoints.Add(i.ToString(), tempDrewPoint);
                    i++;
                }
            }

            DrawAllLines();
            FillAllTriangles();
        }
    }
}
