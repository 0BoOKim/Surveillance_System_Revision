function [BorderLine_blind, BorderLine_eff, X, Y] = get_Sectoral_Coverage_CS(H_AOV, ViewAngleH, X, Y, R_blind, R_eff)
%UNTITLED4 �� �Լ��� ��� ���� ��ġ
%   �ڼ��� ���� ��ġ

% (note) 
% R_blind --> Eff_Dist_From @ CS
% R_eff --> Eff_Dist_To     @ CS


% ������ ���� ����غ� ��:  X��� Y�� �����Ϳ��� �ʿ���.

X1 = R_blind*cos(-H_AOV/2:0.001:H_AOV/2);
Y1 = R_blind*sin(-H_AOV/2:0.001:H_AOV/2);

X2 = R_eff*cos(-H_AOV/2:0.001:H_AOV/2);
Y2 = R_eff*sin(-H_AOV/2:0.001:H_AOV/2);

% fot competibility
X = cast(X, "double");
Y = cast(Y, "double");

% rotation
%ViewAngleH = ViewAngleH + ViewAngleH/2;
BorderLine_blind(:,1) = X1*cos(ViewAngleH)-Y1*sin(ViewAngleH)+X;
BorderLine_blind(:,2) = X1*sin(ViewAngleH)+Y1*cos(ViewAngleH)+Y;


BorderLine_eff(:,1) = X2*cos(ViewAngleH)-Y2*sin(ViewAngleH)+X;
BorderLine_eff(:,2) = X2*sin(ViewAngleH)+Y2*cos(ViewAngleH)+Y;




% shift

end

