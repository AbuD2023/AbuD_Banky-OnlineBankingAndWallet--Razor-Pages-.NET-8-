/// نموذج بيانات المستخدم في تطبيق الهاتف (User Model)
class UserModel {
  final String id;
  final String fullName;
  final String email;
  final String phone;
  final String role;
  final String? profileImage;
  final String kycStatus;
  final String? kycIdFront;
  final String? kycIdBack;
  final String? kycRejectionReason;
  final bool hideFullName;
  final bool hidePhoneOnPos;
  final String? posAliasPhone;
  final bool isBiometricEnabled;
  final bool mustChangePassword;
  final bool isBlocked;
  final String? blockedMessage;
  final DateTime? createdAt;

  UserModel({
    required this.id,
    required this.fullName,
    required this.email,
    required this.phone,
    required this.role,
    this.profileImage,
    required this.kycStatus,
    this.kycIdFront,
    this.kycIdBack,
    this.kycRejectionReason,
    this.hideFullName = false,
    this.hidePhoneOnPos = false,
    this.posAliasPhone,
    this.isBiometricEnabled = false,
    this.mustChangePassword = false,
    this.isBlocked = false,
    this.blockedMessage,
    this.createdAt,
  });

  /// هل الحساب موثق ومقبول من الإدارة
  bool get isApproved => kycStatus == 'Approved';

  /// هل التوثيق قيد مراجعة الإدارة
  bool get isPendingApproval => kycStatus == 'PendingApproval';

  /// هل التوثيق مرفوض
  bool get isRejected => kycStatus == 'Rejected';

  /// هل لم يرفع المستخدم وثائق التوثيق بعد
  bool get isNotSubmitted => kycStatus == 'NotSubmitted';

  /// تحويل من JSON إلى كائن UserModel
  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id']?.toString() ?? '',
      fullName: json['fullName'] ?? '',
      email: json['email'] ?? '',
      phone: json['phone'] ?? '',
      role: json['role'] ?? 'Client',
      profileImage: json['profileImage'],
      kycStatus: json['kycStatus'] ?? 'NotSubmitted',
      kycIdFront: json['kycIdFront'],
      kycIdBack: json['kycIdBack'],
      kycRejectionReason: json['kycRejectionReason'],
      hideFullName: json['hideFullName'] ?? false,
      hidePhoneOnPos: json['hidePhoneOnPos'] ?? false,
      posAliasPhone: json['posAliasPhone'],
      isBiometricEnabled: json['isBiometricEnabled'] ?? false,
      mustChangePassword: json['mustChangePassword'] ?? false,
      isBlocked: json['isBlocked'] ?? false,
      blockedMessage: json['blockedMessage'],
      createdAt: json['createdAt'] != null ? DateTime.tryParse(json['createdAt']) : null,
    );
  }

  /// تحويل كائن UserModel إلى JSON
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fullName': fullName,
      'email': email,
      'phone': phone,
      'role': role,
      'profileImage': profileImage,
      'kycStatus': kycStatus,
      'kycIdFront': kycIdFront,
      'kycIdBack': kycIdBack,
      'kycRejectionReason': kycRejectionReason,
      'hideFullName': hideFullName,
      'hidePhoneOnPos': hidePhoneOnPos,
      'posAliasPhone': posAliasPhone,
      'isBiometricEnabled': isBiometricEnabled,
      'mustChangePassword': mustChangePassword,
      'isBlocked': isBlocked,
      'blockedMessage': blockedMessage,
      'createdAt': createdAt?.toIso8601String(),
    };
  }
}
