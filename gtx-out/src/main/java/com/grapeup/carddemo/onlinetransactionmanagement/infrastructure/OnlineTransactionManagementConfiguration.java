package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.AccountRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.application.CardCrossReferenceRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.application.TransactionRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.application.TransactionService;
import com.grapeup.carddemo.onlinetransactionmanagement.application.TransactionUseCase;
import com.grapeup.carddemo.onlinetransactionmanagement.application.UnitOfWork;
import jakarta.persistence.EntityManager;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class OnlineTransactionManagementConfiguration {

    @Bean
    public AccountRepository accountRepository(EntityManager em) {
        return new JpaAccountRepository(em);
    }

    @Bean
    public CardCrossReferenceRepository cardCrossReferenceRepository(EntityManager em) {
        return new JpaCardCrossReferenceRepository(em);
    }

    @Bean
    public TransactionRepository transactionRepository(EntityManager em) {
        return new JpaTransactionRepository(em);
    }

    @Bean
    public UnitOfWork unitOfWork(EntityManager em) {
        return new JpaUnitOfWork(em);
    }

    @Bean
    public TransactionUseCase transactionUseCase(AccountRepository accounts,
                                                  CardCrossReferenceRepository cards,
                                                  TransactionRepository transactions,
                                                  UnitOfWork unitOfWork) {
        return new TransactionService(accounts, cards, transactions, unitOfWork);
    }
}
