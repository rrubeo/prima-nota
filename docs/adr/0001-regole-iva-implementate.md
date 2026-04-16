# ADR-0001 — Regole IVA implementate nel modulo 08

- **Stato:** Accettato
- **Data:** 2026-04-16
- **Contesto:** Fase 4, modulo 08 (Gestione IVA) — spec §4 (rischio R5 "Conformità fiscale italiana").
- **Audienza:** sviluppatori che manutengono il modulo, commercialista che valida i calcoli prima del go-live.

## Problema

Implementare una gestione IVA italiana per Prima Nota semplificata che sia
sufficientemente corretta per uso gestionale interno, senza pretendere
di sostituire gli adempimenti ufficiali gestiti dal commercialista.

## Decisioni

### 1. Prima nota semplificata: importo riga = lordo

Ogni riga `RigaMovimento` ha un campo `Importo` che rappresenta il
**movimento monetario lordo** (ciò che entra o esce dal conto
finanziario). Se la riga referenzia una `AliquotaIvaId`, l'imponibile e
l'imposta vengono **scorporati** al volo dal lordo usando l'aliquota
indicata, senza salvarli separatamente in DB.

Motivazione: semplicità del modello dati e coerenza con il concetto di
"prima nota monetaria" (non partita doppia). Le quantità persistite
sono il minimo necessario; qualunque decomposizione è derivabile.

Implementazione: `PrimaNota.Domain.Iva.IvaScorporo.Scorpora(lordo, percentuale)`
usa la formula `imponibile = lordo / (1 + pct/100)`, `imposta = lordo - imponibile`,
entrambe arrotondate a 2 decimali con `MidpointRounding.ToEven`.

### 2. Regime IVA per esercizio

`EsercizioContabile` porta tre nuove proprietà:

- `RegimeIva` — `Ordinario` | `Forfettario`
- `PeriodicitaIva` — `Mensile` | `Trimestrale`
- `CoefficienteRedditivitaForfettario` — percentuale 0-100, richiesta
  solo per il regime forfettario

Il regime è **per esercizio**, non globale: un'azienda può cambiare
regime tra un anno e l'altro senza riscrivere lo storico. La modifica
del regime di un esercizio chiuso è vietata dal metodo
`ConfiguraIva()` (invariante).

### 3. Derivazione del registro IVA

Il registro IVA di destinazione di una riga non è un campo manuale
ma è **derivato** al momento della lettura, combinando la causale del
movimento e la natura della categoria della riga:

| Tipo causale    | Natura categoria | Registro       |
|-----------------|------------------|----------------|
| `Incasso`       | `Entrata`        | `Vendite` (*) |
| `Pagamento`     | `Uscita`         | `Acquisti`     |
| `RimborsoNotaSpese` | `Uscita`     | `Acquisti`     |
| altri           | —                | escluso        |

(*) La separazione tra **Vendite** e **Corrispettivi** nella v1 è
euristica: in un'evoluzione successiva si può introdurre una proprietà
`Fonte` (Fattura / Corrispettivo) a livello di causale. Per ora le due
viste vengono alimentate dallo stesso filtro e il commercialista
applicherà la sua classificazione manuale.

Limitazione nota: righe di giroconto (`TipoMovimento.GirocontoInterno`)
sono escluse dai registri IVA, come devono essere.

### 4. Scopo dei registri nell'app

I registri IVA mostrati in `/iva/registri` sono **gestionali**, non
ufficiali. Non hanno numerazione progressiva, non sono stampabili in
forma bollata, non sono inviati all'Agenzia delle Entrate. Servono:

- al titolare per vedere il proprio carico/credito IVA durante il periodo;
- al commercialista come **riconciliazione** contro i registri ufficiali.

La generazione di registri ufficiali, la stampa bollata e l'export
XML-LIPE sono **fuori scope** di v1 e vanno fatti dal software del
commercialista.

### 5. Liquidazione periodica IVA

Per il regime ordinario, `GetLiquidazioneIva` calcola:

```
IVA a debito       = Σ imposta lorda su Vendite + Corrispettivi del periodo
IVA a credito tot. = Σ imposta lorda su Acquisti del periodo
IVA indetraibile   = Σ (imposta × PercentualeIndetraibile/100)
IVA detraibile     = totale − indetraibile
Saldo periodo      = Debito − Detraibile
Credito riportato  = Σ max(0, −Saldo) di tutti i periodi precedenti
                     dello stesso anno
Saldo finale       = Saldo periodo − Credito riportato
```

**Limitazioni coscienti:**

- Non gestiamo il **credito IVA annuale riportato dall'anno precedente**
  (art. 30 DPR 633/72): l'utente può solo partire da zero ogni anno.
  Questo è un trade-off di semplicità: aggiungere il credito di apertura
  richiede un campo editabile per esercizio e una riconciliazione con la
  dichiarazione annuale dell'anno precedente. Verrà valutato in Fase 8
  (reporting).
- I versamenti F24 non vengono emessi dall'app. Se l'utente registra un
  movimento con causale `F24`, quel movimento non partecipa alla
  liquidazione: è la registrazione contabile del **pagamento** del
  debito IVA, non una nuova operazione IVA.
- Il regime forfettario ritorna `Applicabile = false`: nessun calcolo.
  Il calcolo del reddito imponibile forfettario (ricavi × coefficiente −
  contributi) è rimandato a una fase successiva di reporting.

### 6bis. Esigibilità IVA (Immediata / Cassa) e pagamenti parziali

*Aggiunto in fase 4.1 dopo una review del commercialista: la v1 del modulo*
*liquidava sempre in esigibilità immediata, ignorando il regime "IVA per*
*cassa" e trattando il pagamento come un evento singolo. Errato per le*
*fatture differite e, soprattutto, per i regimi opzionali per cassa.*

- **`ConfigurazioneAzienda`** è un **singleton** (PK fisso = 1) che porta i
  parametri aziendali che non appartengono al singolo esercizio: denominazione,
  partita IVA, codice fiscale, indirizzo, contatti e **`EsigibilitaIvaPredefinita`**
  (Immediata | Cassa). Il seeder garantisce la riga al primo avvio. Il regime
  di esigibilità è aziendale perché l'opzione IVA per cassa è un'opzione
  pluriennale a livello di partita IVA, non un attributo del singolo esercizio.

- **`MovimentoPrimaNota.DataCompetenza`** è la data di **competenza IVA**
  (di norma = data documento). Default `= Data` per movimenti digitati a mano;
  impostata esplicitamente quando si importa una fattura XML dove la data
  documento è antecedente alla data registrazione. I registri IVA filtrano
  e ordinano per `DataCompetenza` (non più per `Data`).

- **`MovimentoPrimaNota.Pagamenti[]`** è una collection figlia (tabella
  `PagamentiMovimento`) che modella **pagamenti parziali, acconti e rate**.
  Ogni `PagamentoMovimento` ha `Data`, `Importo` (sempre positivo), `ContoFinanziarioId`
  e una nota libera. Derivati:

  ```
  TotalePagato = Σ Pagamenti.Importo
  Residuo      = |Totale fattura| − TotalePagato
  IsFullyPaid  = Residuo ≤ 0,01 €
  DataPagamento = max(Pagamenti.Data)  se IsFullyPaid, altrimenti null
  ```

  Il vincolo `Importo ≤ Residuo + 0,01 €` impedisce sovra-pagamenti. I
  pagamenti sono ammessi su Draft e Confirmed; su Reconciled la gestione
  spetta al modulo 10 (riconciliazione bancaria).

- **Registro IVA con esigibilità**: anche il registro IVA (`GetRegistroIva`)
  segue l'esigibilità aziendale. Sotto **Immediata** il registro è filtrato
  per `DataCompetenza` (data documento). Sotto **Cassa** il registro emette
  una riga per ogni `(riga × pagamento-in-periodo)` con importi pro-quota,
  `Data = pagamento.Data`. I movimenti senza `Pagamenti[]` esplicita
  (vendite cash / corrispettivi registrati come singola transazione che
  tocca direttamente Cassa/Banca) ricadono su `Data = m.Data` con importo
  intero. In questo modo la vista del registro rispecchia esattamente quello
  che concorre alla liquidazione del periodo.

- **Liquidazione periodica con esigibilità**:

  - **Immediata**: IVA esigibile **a prescindere dal pagamento**, sulla
    data di competenza. Formula invariata (vedi §5), ma i registri ora
    sono filtrati su `DataCompetenza in [dataInizio, dataFine]`.

  - **Cassa**: IVA esigibile **solo al momento dell'incasso/pagamento**,
    proporzionalmente all'importo incassato/pagato. Per ogni fattura con
    almeno un `Pagamento` nel periodo:

    ```
    ratio = Σ Pagamenti_in_periodo.Importo / |Totale fattura|
    IVA pro-quota riga = scorpora(|riga.Importo|, aliquota.%).imposta × ratio
    ```

    Le righe con natura `Entrata` contribuiscono al debito; quelle con
    natura `Uscita` al credito (con la consueta detrazione per
    `PercentualeIndetraibile`). Il `ratio` è capped a 1 per sicurezza
    (acconti superiori al totale in presenza di note di credito non
    ancora rappresentate).

- **Limitazioni coscienti della v1 per cassa**:
  - **Cap 12 mesi** (ex art. 32-bis DL 83/2012): dopo un anno
    dall'emissione della fattura, l'IVA diventa comunque esigibile
    anche se non incassata. Non implementato: richiede un secondo
    criterio di inclusione (fatture con `DataCompetenza ≤ periodo − 12 mesi`
    il cui Residuo è ancora > 0). Valutare in Fase 8 sul parere del
    commercialista.
  - Le **note di credito** oggi si registrano come movimento separato
    con segno opposto: non esiste ancora un link esplicito "rettifica
    di fattura X". Sotto cassa, la nota di credito su una fattura già
    (parzialmente) incassata richiederà una gestione dedicata.
  - Il **regime forfettario** ignora del tutto l'esigibilità (nessuna IVA).

### 6. Percentuale di indetraibilità per riga → regola dell'aliquota

La percentuale di indetraibilità IVA non è impostata per riga, ma vive
sull'`AliquotaIva` stessa (`PercentualeIndetraibile`). Motivazione:

- Nella prassi italiana l'indetraibilità si applica per categoria merceologica
  (es. auto aziendale al 40%, spese di rappresentanza al 100%, ecc.)
  e si modella meglio creando aliquote dedicate ("IVA 22% auto 40% ded",
  "IVA 22% rappresentanza 100% ind").
- Questa scelta tiene le righe semplici e concentra la logica IVA nell'anagrafica aliquote.

## Conseguenze

- L'app produce numeri **coerenti e tracciabili** con i movimenti di
  prima nota, pronti per la riconciliazione con il commercialista.
- **Non** sostituisce adempimenti ufficiali. Si consiglia di menzionarlo
  esplicitamente al titolare prima del go-live.
- Le regole sono **rivisitabili** senza migrazione dati: il calcolo è on-the-fly,
  nessun valore scorporato è persistito.

## Azioni di follow-up

- [ ] Presentare questo ADR al commercialista e annotare deviazioni
- [ ] Valutare in Fase 8 il credito IVA di apertura e l'export per la dichiarazione annuale
- [ ] Quando arriva la Fase 7 (note spese), confermare che i rimborsi nota spese con IVA confluiscono correttamente negli Acquisti
- [ ] Modulo 10 (riconciliazione) non deve alterare gli importi delle righe, solo il link a estratto conto
- [ ] CRUD UI per `ConfigurazioneAzienda` (`/admin/azienda`) e per i pagamenti parziali (pannello nel form movimento); scheda cliente/fornitore come report dedicato
- [ ] Cap 12 mesi IVA per cassa (art. 32-bis DL 83/2012) dopo parere commercialista
- [x] Ventilazione corrispettivi (art. 24 c. 3 DPR 633/72): **esclusa deliberatamente** — ogni riga movimento porta l'aliquota IVA esplicita, non servono corrispettivi a aliquota mista ripartiti in proporzione agli acquisti
- [ ] Link esplicito nota-di-credito → fattura per corretta rettifica pro-quota sotto Cassa
