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

            cameraTransformMatrix = calculateCameraTransformMatrix();
        }

        private Matrix<double> calculateCameraTransformMatrix()
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
    }
}
