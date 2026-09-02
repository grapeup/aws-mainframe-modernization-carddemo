package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.AddTransactionInput;
import com.grapeup.carddemo.onlinetransactionmanagement.application.AddTransactionResult;
import com.grapeup.carddemo.onlinetransactionmanagement.application.PaymentInput;
import com.grapeup.carddemo.onlinetransactionmanagement.application.PaymentResult;
import com.grapeup.carddemo.onlinetransactionmanagement.application.TransactionUseCase;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/transactions")
public class OnlineTransactionManagementController {

    private final TransactionUseCase transactionUseCase;

    public OnlineTransactionManagementController(TransactionUseCase transactionUseCase) {
        this.transactionUseCase = transactionUseCase;
    }

    @PostMapping
    public ResponseEntity<AddTransactionResult> addTransaction(@RequestBody AddTransactionInput input) {
        AddTransactionResult result = transactionUseCase.addTransaction(input);
        if (result.success()) {
            return ResponseEntity.ok(result);
        }
        return ResponseEntity.badRequest().body(result);
    }

    @PostMapping("/payments")
    public ResponseEntity<PaymentResult> makePayment(@RequestBody PaymentInput input) {
        PaymentResult result = transactionUseCase.makePayment(input);
        if (result.success()) {
            return ResponseEntity.ok(result);
        }
        return ResponseEntity.badRequest().body(result);
    }
}
