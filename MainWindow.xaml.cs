using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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

        List<Vector<double>> projectedPoints = new List<Vector<double>> { };

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                renderData = LoatDataFromJson.LoadRenderDataFromJson("C:\\Users\\WINDOWS11\\Documents\\Apuntes IAV 7 Semestre\\ComputacionGrafica\\Tarea2\\drawConfig.json");
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
                foreach (Vector<double> projectePoints in projectedPoints) 
                {
                    var (x, y) = GetDevicePointFromProjection(projectePoints);

                    Rectangle pixelToDraw = new Rectangle{ Fill = Brushes.White, Width = 1, Height = 1};

                    Canvas.SetLeft(pixelToDraw, x); Canvas.SetTop(pixelToDraw, y);

                    Device.Children.Add(pixelToDraw);
                }
                
            }
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

        private Tuple<double, double, double, double> GetCanvasSize()
        {
            double xDMax = Device.ActualWidth;
            double xDMin = 1;

            // El origen del canvas de WPF se pinta desde arriba a la derecha, por lo que yDMax y yDMin se invierten.
            double yDMax = 1;
            double yDMin = Device.ActualHeight;

            return Tuple.Create(xDMin, xDMax, yDMin, yDMax);
        }
    }
}
