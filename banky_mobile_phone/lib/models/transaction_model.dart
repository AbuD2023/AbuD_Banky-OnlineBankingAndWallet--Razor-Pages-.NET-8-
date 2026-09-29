/// نموذج الحركة المالية وسجل العمليات في تطبيق الهاتف
class TransactionModel {
  final String id;
  final String transactionNumber;
  final String type;
  final String typeNameAr;
  final String currencyCode;
  final String currencySymbol;
  final double amount;
  final double fee;
  final double totalAmount;
  final String status;
  final String statusNameAr;
  final String? senderDisplayName;
  final String? senderDisplayPhone;
  final String? receiverDisplayName;
  final String? posName;
  final String? note;
  final bool isIncoming;
  final DateTime? createdAt;

  TransactionModel({
    required this.id,
    required this.transactionNumber,
    required this.type,
    required this.typeNameAr,
    required this.currencyCode,
    required this.currencySymbol,
    required this.amount,
    required this.fee,
    required this.totalAmount,
    required this.status,
    required this.statusNameAr,
    this.senderDisplayName,
    this.senderDisplayPhone,
    this.receiverDisplayName,
    this.posName,
    this.note,
    required this.isIncoming,
    this.createdAt,
  });

  factory TransactionModel.fromJson(Map<String, dynamic> json) {
    return TransactionModel(
      id: json['id']?.toString() ?? '',
      transactionNumber: json['transactionNumber'] ?? '',
      type: json['type'] ?? '',
      typeNameAr: json['typeNameAr'] ?? json['type'] ?? '',
      currencyCode: json['currencyCode'] ?? '',
      currencySymbol: json['currencySymbol'] ?? json['currencyCode'] ?? '',
      amount: (json['amount'] is num) ? (json['amount'] as num).toDouble() : 0.0,
      fee: (json['fee'] is num) ? (json['fee'] as num).toDouble() : 0.0,
      totalAmount: (json['totalAmount'] is num) ? (json['totalAmount'] as num).toDouble() : 0.0,
      status: json['status'] ?? 'Completed',
      statusNameAr: json['statusNameAr'] ?? 'ناجحة',
      senderDisplayName: json['senderDisplayName'],
      senderDisplayPhone: json['senderDisplayPhone'],
      receiverDisplayName: json['receiverDisplayName'],
      posName: json['posName'],
      note: json['note'],
      isIncoming: json['isIncoming'] ?? false,
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
    );
  }
}
