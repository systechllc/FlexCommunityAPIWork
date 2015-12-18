#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QUdpSocket>
#include <QTcpSocket>

namespace Ui {
  class MainWindow;
}

class MainWindow : public QMainWindow
{
    Q_OBJECT

  public:
    explicit MainWindow(QWidget *parent = 0);
    ~MainWindow();

  public slots:
    void readDiscoveryDatagrams();

  private:
    Ui::MainWindow *ui;

    QUdpSocket *udpDiscoverySocket;

    void processDiscoveryDatagram( QByteArray &dg );

};

#endif // MAINWINDOW_H
