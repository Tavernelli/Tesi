package com.clientandroid;

import android.os.Bundle;
import android.support.v7.app.AppCompatActivity;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;

public class MainActivity extends AppCompatActivity {

    Button buttonReceve;
    Button clear;
    EditText editTextAddress, editTextPort;
    TextView responseName;
    TextView responseWidth;
    TextView responseHeight;
    String address;
    String port ;


    @Override
    protected void onCreate(final Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        buttonReceve = (Button) findViewById(R.id.button);
        editTextAddress = (EditText) findViewById(R.id.addressEditText);
        editTextPort = (EditText) findViewById(R.id.portEditText);
        responseName = (TextView) findViewById(R.id.responseNameTextView);
        responseWidth = (TextView) findViewById(R.id.responseWidthTextView);
        responseHeight = (TextView) findViewById(R.id.responseHeightTextView);
        clear = (Button) findViewById(R.id.button2);


        buttonReceve.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {


                    address = editTextAddress.getText().toString();
                    port = editTextPort.getText().toString();


                if(address.isEmpty() || address.length() == 0 || address.equals("")){
                    editTextAddress.setError("Errore");

                    return;


                }
                else if (port.isEmpty() || port.length() == 0 || port.equals(""))
                {
                    editTextPort.setError("Errore");
                    return;
                }

                else
                {
                    Client task = new Client(address, Integer.parseInt(port), responseName,
                            responseWidth,
                            responseHeight);
                    task.execute();}

            }

        });

        clear.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                responseName.setText("");
                responseWidth.setText("");
                responseHeight.setText("");

            }

        });
    }

}


