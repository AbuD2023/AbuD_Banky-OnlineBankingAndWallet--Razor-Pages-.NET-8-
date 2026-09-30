/// نموذج نقطة البيع (POS Point) في تطبيق الهاتف
class PosModel {
  final String id;
  final String name;
  final String posCode;
  final String? address;
  final String? category;
  final bool isActive;
  final double totalReceivedAmount;
  final int totalTransactionsCount;
  final DateTime? createdAt;

  PosModel({
    required this.id,
    required this.name,
    required this.posCode,
    this.address,
    this.category,
    required this.isActive,
    required this.totalReceivedAmount,
    required this.totalTransactionsCount,
    this.createdAt,
  });

  factory PosModel.fromJson(Map<String, dynamic> json) {
    return PosModel(
      id: json['id']?.toString() ?? '',
      name: json['name'] ?? '',
      posCode: json['posCode'] ?? '',
      address: json['address'],
      category: json['category'],
      isActive: json['isActive'] ?? true,
      totalReceivedAmount: (json['totalReceivedAmount'] is num)
          ? (json['totalReceivedAmount'] as num).toDouble()
          : 0.0,
      totalTransactionsCount: json['totalTransactionsCount'] is int
          ? json['totalTransactionsCount']
          : 0,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'])
          : null,
    );
  }
}

/// استجابة الاستعلام عن مستلم برقم الهاتف
class RecipientLookupResult {
  final String clientId;
  final String displayName;
  final String phone;
  final bool isNameMasked;
  final bool hasActiveWalletInCurrency;
  final double estimatedFee;
  final String? feeDescription;

  RecipientLookupResult({
    required this.clientId,
    required this.displayName,
    required this.phone,
    required this.isNameMasked,
    required this.hasActiveWalletInCurrency,
    this.estimatedFee = 0.0,
    this.feeDescription,
  });

  factory RecipientLookupResult.fromJson(Map<String, dynamic> json) {
    return RecipientLookupResult(
      clientId: json['clientId']?.toString() ?? '',
      displayName: json['displayName'] ?? '',
      phone: json['phone'] ?? '',
      isNameMasked: json['isNameMasked'] ?? false,
      hasActiveWalletInCurrency: json['hasActiveWalletInCurrency'] ?? false,
      estimatedFee: (json['estimatedFee'] is num) ? (json['estimatedFee'] as num).toDouble() : 0.0,
      feeDescription: json['feeDescription'],
    );
  }
}

/// استجابة الاستعلام عن نقطة بيع
class PosLookupResult {
  final String posId;
  final String name;
  final String posCode;
  final String? category;
  final String? address;
  final String merchantDisplayName;
  final bool isActive;
  final double estimatedFee;
  final String? feeDescription;

  PosLookupResult({
    required this.posId,
    required this.name,
    required this.posCode,
    this.category,
    this.address,
    required this.merchantDisplayName,
    required this.isActive,
    this.estimatedFee = 0.0,
    this.feeDescription,
  });

  factory PosLookupResult.fromJson(Map<String, dynamic> json) {
    return PosLookupResult(
      posId: json['posId']?.toString() ?? '',
      name: json['name'] ?? '',
      posCode: json['posCode'] ?? '',
      category: json['category'],
      address: json['address'],
      merchantDisplayName: json['merchantDisplayName'] ?? '',
      isActive: json['isActive'] ?? true,
      estimatedFee: (json['estimatedFee'] is num) ? (json['estimatedFee'] as num).toDouble() : 0.0,
      feeDescription: json['feeDescription'],
    );
  }
}
