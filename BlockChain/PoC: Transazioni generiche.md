# PoC: Transazioni generiche
###### Attuare scambi di risorse tra due o più parti su una blockchain pubblica e permissionless

Supponiamo che Alice voglia mandare 10 mele a Bob. Alice compilerà la seguente richiesta di transazione:  
  
**From:** [Chiave pubblica di Alice]  
**To:** [Chiave pubblica di Bob]  
**AmountToSend:**[Quantità da inviare]  
**ObjectToSend:**[Oggetto da inviare]  
**SenderSig:**[Firma della richiesta di transazione di Alice]  
  
Alice invierà in broadcast ai peer a lei connessi la transazione.
I peer procederanno a verificare l'autenticità della richiesta analizzando la firma, calcolata sulla richiesta di transazione corrente,
e poi broadcasteranno a loro volta la richiesta fino a Bob, senza memorizzarla.

Bob analizzerà a sua volta l'autenticità della richiesta, poi apporrà le sue condizioni sulla transazione:
  
**From:** [Chiave pubblica di Alice]  
**To:** [Chiave pubblica di Bob]  
**AmountToSend:**[Quantità da inviare]  
**ObjectToSend:**[Oggetto da inviare]  
**AmountToReceive:**[Quantità da ricevere]  
**ObjectToReceive:**[Oggetto da ricevere]  
**SenderSig:**[Firma della richiesta di transazione di Alice]  
**ReceiverSig:**[Firma della risposta di transazione di Bob]  
  
Dopo che Bob ha inserito le sue condizioni sulla risposta di transazione,
applicherà la sua firma e reinvierà la risposta al network.
La firma è apposta sulla risposta in modo che sia non solo verificabile che Bob sia d'accordo con la transazione, ma anche che la firma di Alice sia autentica,
così che Bob non possa creare una finta risposta di transazione.
  
Quando la risposta arriva ad Alice, questa verificherà che le firme siano valide e che le condizioni siano giuste:  
- Se le condizioni sono accettate da Alice, la transazione viene ufficializzata e una nuova firma viene apposta da Alice, per poi inviare la conferma al network e quindi a Bob. La transazione verrà validata dal network e inserita in un blocco.
- Se le condizioni non sono accettate, la risposta viene eliminata*.
  
Struttura della transazione finalizzata:  
  
**From:** [Chiave pubblica di Alice]  
**To:** [Chiave pubblica di Bob]  
**AmountToSend:**[Quantità da inviare]  
**ObjectToSend:**[Oggetto da inviare]  
**AmountToReceive:**[Quantità da ricevere]  
**ObjectToReceive:**[Oggetto da ricevere]  
**SenderSig:**[Firma della richiesta di transazione di Alice]  
**ReceiverSig:**[Firma della risposta di transazione di Bob]  
**FinalSenderSig:**[Firma di conferma della transazione di Alice]  
  
====
###Problemi noti  
Questo metodo di implementazione richiede che il nodo di Bob sia online al momento dell'arrivo della richiesta di Alice.
  
*E' ancora da definire un modo per negoziare sulle condizioni qualora queste non venissero accettate al primo round.
