/// نموذج المحفظة المالية للعميل في تطبيق الهاتف
class WalletModel {
  final String id;
  final String accountNumber;
  final String currencyCode;
  final String currencyNameAr;
  final String currencyNameEn;
  final String currencySymbol;
  final double balance;
  final bool isActive;
  final DateTime? createdAt;

  WalletModel({
    required this.id,
    required this.accountNumber,
    required this.currencyCode,
    required this.currencyNameAr,
    required this.currencyNameEn,
    required this.currencySymbol,
    required this.balance,
    required this.isActive,
    this.createdAt,
  });

  factory WalletModel.fromJson(Map<String, dynamic> json) {
    return WalletModel(
      id: json['id']?.toString() ?? '',
      accountNumber: json['accountNumber'] ?? '',
      currencyCode: json['currencyCode'] ?? '',
      currencyNameAr: json['currencyNameAr'] ?? json['currencyCode'] ?? '',
      currencyNameEn: json['currencyNameEn'] ?? json['currencyCode'] ?? '',
      currencySymbol: json['currencySymbol'] ?? json['currencyCode'] ?? '',
      balance: (json['balance'] is num) ? (json['balance'] as num).toDouble() : 0.0,
      isActive: json['isActive'] ?? true,
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'accountNumber': accountNumber,
      'currencyCode': currencyCode,
      'currencyNameAr': currencyNameAr,
      'currencyNameEn': currencyNameEn,
      'currencySymbol': currencySymbol,
      'balance': balance,
      'isActive': isActive,
      'createdAt': createdAt?.toIso8601String(),
    };
  }
}

/// نموذج العملة المتاحة للإنشاء
class CurrencyModel {
  final int id;
  final String code;
  final String nameAr;
  final String nameEn;
  final String symbol;
  final bool isActive;
  final double exchangeRate;

  CurrencyModel({
    required this.id,
    required this.code,
    required this.nameAr,
    required this.nameEn,
    required this.symbol,
    required this.isActive,
    required this.exchangeRate,
  });

  factory CurrencyModel.fromJson(Map<String, dynamic> json) {
    return CurrencyModel(
      id: json['id'] is int ? json['id'] : 0,
      code: json['code'] ?? '',
      nameAr: json['nameAr'] ?? '',
      nameEn: json['nameEn'] ?? '',
      symbol: json['symbol'] ?? '',
      isActive: json['isActive'] ?? true,
      exchangeRate: (json['exchangeRate'] is num) ? (json['exchangeRate'] as num).toDouble() : 1.0,
    );
  }
}
