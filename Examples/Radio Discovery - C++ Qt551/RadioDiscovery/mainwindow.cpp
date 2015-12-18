#include "mainwindow.h"
#include "ui_mainwindow.h"

//
// This is a very simple C++ program under Qt 5.5.1 that simply creates the
// required UDP socket to listen for radio UDP broadcasts used by the radio
// and software for discovery.
//
// To build and run this example do the following:
//
// Download the free community version of Qt V5.5.1.  Goto  http://www.qt.io/download/
// Answers: Open source distribution under LGPL or GPL license and answer the rest of the questions
// You will probably also need to setup an account on qt so when you install it will know who you are.
// Don't worry they don't bug you about things.  Once you get past the questions at the bottom of the downloads page
// is view all downloads.  Pick the one you like the most:
//
// If you already have Visual Studio 2013 installed then pick: Qt 5.5.1 for Windows 32-bit (VS 2013, 804 MB)
// If you would like to use the free MinGW compiler then pick: Qt 5.5.1 for Windows 32-bit (MinGW 4.9.2, 1.0 GB)
//
// This last one installs not only Qt but the compiler as well for you.
//
// Once everything is installed run Qt Creator and load up this program.  It may ask you to configure for a compiler
// and it should show you the one you selected.   Assuming all goes well hit the green arrow in the lower left and it
// should build and run.
//
// This program works under Windows and MacOS for sure. It may work under linux but I don't have a linux instance
// to try it under.
//
// Under Qt you have slots and signals.  When a component has something ready
// it sends a signal.  You connect that signal to a slot.  Below the code connects
// the "readyRead()" signal to a slot called "readDiscoveryDatagrams()".
//
// Once this connect is made any incoming datagram from the radio is sent to the
// function below (slot) called readDiscoveryDatagrams().  This function reads a datagram
// and if it gets one passes it on to processDiscoveryDatagram().
//
// Process discovery datagram takes the data packet, extracts 28 bytes of header data
// then displays in the list box the header in hex and the rest of the data from the radio.
//
// If you watch it run you will see data about your radio in the display.
//
// This is a very simple program that does nothing more than display the contents of the datagram from the radio.
// It does not try to look into the header data nor does it do anything with the radio data but dump it to the list box.
//


MainWindow::MainWindow(QWidget *parent) :
  QMainWindow(parent),
  ui(new Ui::MainWindow),
  udpDiscoverySocket(nullptr)
{
  ui->setupUi(this);

  udpDiscoverySocket = new QUdpSocket(this);
  udpDiscoverySocket->bind(QHostAddress::Any, 4992);

  connect(udpDiscoverySocket, SIGNAL(readyRead()), this, SLOT(readDiscoveryDatagrams()));

}

MainWindow::~MainWindow()
{
  delete ui;
}

void MainWindow::readDiscoveryDatagrams()
{
    while (udpDiscoverySocket->hasPendingDatagrams())
    {
        QByteArray datagram;
        datagram.resize(udpDiscoverySocket->pendingDatagramSize());
        QHostAddress sender;
        quint16 senderPort;

        udpDiscoverySocket->readDatagram(datagram.data(), datagram.size(), &sender, &senderPort);

        processDiscoveryDatagram(datagram);
    }
}

QString ByteArrayToHexString( QByteArray &array, qint32 num )
{
  QString Result;

  char buffer[5];
  memset( buffer, 0, 5 );

  if( num==0 )
    num = array.size();

  for(int c=0; c<num; c++)
  {
    sprintf_s( buffer, 3, "%02X", array.at(c) & 0xff );
    Result.append( buffer );
    Result.append( " " );
  }

  return Result.trimmed();
}

void MainWindow::processDiscoveryDatagram( QByteArray &dg )
{
  QByteArray prefix = dg.left(28);
  dg.remove(0,28);

  QList<QByteArray> list = dg.split(' ');

  if( list.count()>0 )
  {
    ui->listWidget->addItem( "-------- Discovery Datagram -------" );
    ui->listWidget->addItem( QString("prefix=%1").arg(ByteArrayToHexString(prefix,prefix.length())) );
    for( int c=0; c<list.count(); c++ )
    {
      QString item = list.at(c);

      ui->listWidget->addItem( item );
    }
  }
}

