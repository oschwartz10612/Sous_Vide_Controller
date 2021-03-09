#!/usr/local/bin/python3
 
import serial, io, time
 
device   = '/dev/cu.usbserial-1420' # serial port
baud     = 9600                          # baud rate
filename = 'temp-log.txt'                # log file to save data in

with serial.Serial(device,baud) as serialPort, open(filename,'wb') as outFile:
    line = serialPort.readline() # must send \n! get a line of log
    time.sleep(3)
    serialPort.write(b'55')
    while(1):
        line = serialPort.readline() # must send \n! get a line of log
        print (line)                 # show line on screen
        outFile.write(line)          # write line of text to file
        outFile.flush()              # make sure it actually gets written out