package com.clientandroid;

import android.os.AsyncTask;
import android.widget.TextView;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.IOException;
import java.net.Socket;
import java.net.UnknownHostException;

public class Client extends AsyncTask<Void, Void, Void> {
    java.io.DataInputStream dataInputStream;
    DataOutputStream dataOutputStream = null;
    Socket socket;
    String[] output = null;
    String dstAddress;
    int dstPort;
    String message="";
    TextView textResponseName;
    TextView textResponseWidth;
    TextView textResponseHeight;

    Client(String addr, int port, TextView textResponseName, TextView textResponseWidth, TextView textResponseHeight) {
        dstAddress = addr;
        dstPort = port;
        this.textResponseName = textResponseName;
        this.textResponseWidth = textResponseWidth;
        this.textResponseHeight = textResponseHeight;

    }



    @Override
    protected Void doInBackground(Void... params) {
        byte[] messageByte = new byte[4096];
        boolean end = false;


        try {
            socket = new Socket(dstAddress, dstPort);
            //Send message to the server
            dataOutputStream = new DataOutputStream(socket.getOutputStream());
            dataOutputStream.writeUTF("Hello Kinect!");
            int bytesRead = 0;

            //Receive message from the server
            dataInputStream = new DataInputStream(socket.getInputStream());


            while(!end)
            {
                bytesRead = dataInputStream.read(messageByte);
                message = new String(messageByte, 0, bytesRead);
                if (message.length() == bytesRead)
                {
                    end = true;
                }
            }



        } catch (UnknownHostException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        } finally {
            if (socket != null) {
                try {
                    socket.close(); //close the connection
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
            if (dataOutputStream != null) {
                try {
                    dataOutputStream.close(); //close the output stream
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }
            if (dataInputStream != null) {
                try {
                    dataInputStream.close(); //close the input stream
                } catch (IOException e) {
                    e.printStackTrace();
                }
            }

        }

    return  null;
    }

    @Override
    protected void onPostExecute(Void result) {

    //    System.out.println("ciao" + message);
        if (!message.equals("--")) {
            output = message.split("-") ;
            textResponseName.setText(output[0]);
            textResponseWidth.setText(output[1]);
            textResponseHeight.setText(output[2]);
        } else {
            textResponseName.setText("");
            textResponseWidth.setText("");
            textResponseHeight.setText("");
        }



        super.onPostExecute(result);
    }
}