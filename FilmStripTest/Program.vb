Option Strict On
Option Explicit On
Option Infer On

Imports System.Windows.Forms

Friend Module Program

    <STAThread>
    Friend Sub Main()
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Form1())
    End Sub

End Module
