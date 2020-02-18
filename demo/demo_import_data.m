N = 249;
m = 512;
n = 424;

depth_frames = zeros(m, n, N);
ir_frames = zeros(m, n, N);

for i=1:N
    load(['Xef2Mat_Output\DepthFrame' num2str(i-1,'%04d')]);
    var_name = ['Dep' num2str(i-1,'%04d') '_'];
    depth_frames(:, :, i) = eval(var_name);
    clear(var_name);
end

for i=1:N
    load(['Xef2Mat_Output\IRFrame' num2str(i-1,'%04d')]);
    var_name = ['IR' num2str(i-1,'%04d') '__'];
    ir_frames(:, :, i) = eval(var_name);
    clear(var_name);
end

load('Xef2Mat_Output\TimeStamp.mat');
clear var_name i

for i=1:N
    imshow(rot90(depth_frames(:,:,i), -1),[0 8000]);
    pause(0.0001);
end